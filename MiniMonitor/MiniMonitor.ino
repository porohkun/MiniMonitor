/*
 Name:		MiniMonitor.ino
 Created:	8/20/2020 8:14:07 PM
 Author:	Poroh
*/

#include "Arduino.h"
#include "USBAPI.h"
#include "src\Button.h"
#include "src\Timer.h"
#include "modbusino\Modbusino.h"
#include "modbusino\Modbusino.cpp"
#include <U8g2lib.h>

U8G2_SSD1306_128X64_NONAME_F_HW_I2C u8g2(U8G2_R0, /* reset=*/U8X8_PIN_NONE);
ModbusinoSlave Modbus(1);

#define MODE_BUTTON 0x04

#define ID_0 0x4D // this defines
#define ID_1 0x4D // gadget name for
#define ID_2 0x72 // identify COM port on PC

#define REG_YEAR 0x03
#define REG_MONTH 0x04
#define REG_DAY 0x05
#define REG_HOUR 0x06
#define REG_MINUTE 0x07
#define REG_SECOND 0x08
#define REG_CPU_TEMP 0x09
#define REG_GPU_TEMP 0x0A

#define ZERO 0x00
#define CPU_MIN_TEMP 40
#define CPU_MAX_TEMP 60
#define GPU_MIN_TEMP 40
#define GPU_MAX_TEMP 60

#define CPU_LABEL "CPU"
#define GPU_LABEL "GPU"

uint16_t _registers[11]{
	ID_0, // 0: M
	ID_1, // 1: M
	ID_2, // 2: r
	ZERO, // 3: year
	ZERO, // 4: month
	ZERO, // 5: day
	ZERO, // 6: hour
	ZERO, // 7: minute
	ZERO, // 8: second
	ZERO, // 9: cpu temp
	ZERO  // 10: gpu temp
};
uint8_t _regSize = sizeof(_registers) / sizeof(_registers[0]);

bool _cpuMode = true;
uint8_t _cpuLine[22]{ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO};
uint8_t _gpuLine[22]{ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO};
uint8_t _lineSize = sizeof(_cpuLine) / sizeof(_cpuLine[0]);
uint8_t _lineIndex = 0;

void setup()
{
	_registers[0] = ID_0;
	_registers[1] = ID_1;
	_registers[2] = ID_2;

	byte buttons[] = {MODE_BUTTON};
	Button.SetButtons(buttons);

	Timer.SetInterval(1000);
	Timer.Start();

	u8g2.begin();

	Modbus.setup(115200);
}

uint16_t clamp(uint16_t x, uint16_t lower, uint16_t upper)
{
	return min(upper, max(x, lower));
}

void loop()
{
	Button.Loop();
	if (Button.GetDown(MODE_BUTTON))
		_cpuMode = !_cpuMode;

	Modbus.loop(_registers, 11);
	Timer.Loop();

	_registers[0] = ID_0;
	_registers[1] = ID_1;
	_registers[2] = ID_2;

	if (Timer.IsRaised)
	{
		int16_t tcpu = clamp(_registers[REG_CPU_TEMP], CPU_MIN_TEMP, CPU_MAX_TEMP);
		int16_t tgpu = clamp(_registers[REG_GPU_TEMP], GPU_MIN_TEMP, GPU_MAX_TEMP);
		_cpuLine[_lineIndex] = (tcpu - CPU_MIN_TEMP) * 16 / (CPU_MAX_TEMP - CPU_MIN_TEMP);
		_gpuLine[_lineIndex] = (tgpu - GPU_MIN_TEMP) * 16 / (GPU_MAX_TEMP - GPU_MIN_TEMP);
		_lineIndex = (_lineIndex + 1) % _lineSize;
	}

	u8g2.clearBuffer();

	u8g2.setColorIndex(1);

	drawSideTemp(CPU_LABEL, _registers[REG_CPU_TEMP], 25);
	drawSideTemp(GPU_LABEL, _registers[REG_GPU_TEMP], 49);

	u8g2.setFont(u8g2_font_rosencrantz_nbp_tr);
	u8g2.drawStr(0, 15, _cpuMode ? CPU_LABEL : GPU_LABEL);
	if (Serial)
		u8g2.drawStr(0, 25, "ser");
	if (Serial.available())
		u8g2.drawStr(0, 35, "ser aval");

	u8g2.drawStr(75, 26, "E");

	char temp[3] = "00";
	setNumberToCharBuffer((millis() / 1000) % 100, 0, temp);
	u8g2.drawStr(0, 45, temp);

	setNumberToCharBuffer(_lineIndex, 0, temp);
	u8g2.drawStr(30, 45, temp);

	setNumberToCharBuffer(_gpuLine[_lineIndex], 0, temp);
	u8g2.drawStr(60, 45, temp);

	setNumberToCharBuffer(_registers[REG_HOUR], 0, temp);
	u8g2.drawStr(0, 64, temp);

	setNumberToCharBuffer(_registers[REG_MINUTE], 0, temp);
	u8g2.drawStr(30, 64, temp);

	for (uint8_t i = 0; i < _lineSize; i++)
	{
		uint8_t height = min(1, (_cpuMode ? _cpuLine : _gpuLine)[(_lineIndex + i) % _lineSize]);
		u8g2.drawBox(128 - (_lineSize - i) * 5 + 1, 16 - height, 4, height);
	}

	u8g2.sendBuffer();
}

void drawSideTemp(const char *label, uint16_t value, uint8_t y)
{
	char temp[3] = "00";

	u8g2.setFont(u8g2_font_bitcasual_tu);
	u8g2.drawStr(100, y, label);

	setNumberToCharBuffer(value, 0, temp);
	u8g2.setFont(u8g2_font_timR12_tn);

	u8g2.drawStr(100, y + 14, temp);
	u8g2.drawCircle(125, y + 4, 2);
}

void reverse(char s[])
{
	int i, j;
	char c;

	for (i = 0, j = strlen(s) - 1; i < j; i++, j--)
	{
		c = s[i];
		s[i] = s[j];
		s[j] = c;
	}
}

void itoa(int n, char s[], uint16_t base)
{
	int i, sign;

	if ((sign = n) < 0) /* записываем знак */
		n = -n;			/* делаем n положительным числом */
	i = 0;
	do
	{							 /* генерируем цифры в обратном порядке */
		s[i++] = n % base + '0'; /* берем следующую цифру */
	} while ((n /= base) > 0);	 /* удаляем */
	if (sign < 0)
		s[i++] = '-';
	s[i] = '\0';
	reverse(s);
}

void setNumberToCharBuffer(uint16_t number, uint8_t offset, char *buffer)
{
	char buf[4];
	itoa(number, buf, 10);
	buffer[0 + offset] = number >= 10 ? buf[0] : '0';
	buffer[1 + offset] = number < 10 ? buf[0] : buf[1];
}
