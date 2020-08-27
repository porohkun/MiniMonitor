/*
 Name:		MiniMonitor.ino
 Created:	8/20/2020 8:14:07 PM
 Author:	Poroh
*/



#include "Button.h"
#include "Timer.h"
#include "Modbusino.h"
#include <U8g2lib.h>

U8G2_SSD1306_128X64_NONAME_F_HW_I2C u8g2(U8G2_R0, /* reset=*/ U8X8_PIN_NONE);
ModbusinoSlave Modbus(1);

#define ID_0 0x4D //this defines
#define ID_1 0x4D //gadget name for
#define ID_2 0x72 //identify COM port on PC

#define REG_YEAR     0x03
#define REG_MONTH    0x04
#define REG_DAY      0x05
#define REG_HOUR     0x06
#define REG_MINUTE   0x07
#define REG_SECOND   0x08
#define REG_CPU_TEMP 0x09
#define REG_GPU_TEMP 0x0A

#define ZERO 0x00
#define CPU_MIN_TEMP 40
#define CPU_MAX_TEMP 60

#define CPU_LABEL "CPU"
#define GPU_LABEL "GPU"

uint16_t _registers[11]
{
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
	ZERO  //10: gpu temp
};
uint8_t _regSize = sizeof(_registers) / sizeof(_registers[0]);

bool _cpuMode = true;
uint8_t _cpuLine[22]{ ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO };
uint8_t _cpuLineSize = sizeof(_cpuLine) / sizeof(_cpuLine[0]);
uint8_t _cpuIndex = 0;

void setup()
{
	_registers[0] = ID_0;
	_registers[1] = ID_1;
	_registers[2] = ID_2;

	pinMode(LED_BUILTIN, OUTPUT);

	byte buttons[] = { 0x03 };
	Button.SetButtons(buttons);

	Timer.SetInterval(1000);
	Timer.Start();

	u8g2.begin();

	Modbus.setup(115200);
}

void loop()
{
	Button.Loop();
	digitalWrite(LED_BUILTIN, Button.GetState(0x03) ? HIGH : LOW);

	Modbus.loop(_registers, 11);
	Timer.Loop();

	if (Timer.IsRaised)
	{
		int16_t t = max(0, _registers[REG_CPU_TEMP] - CPU_MIN_TEMP) * 16;
		_cpuLine[_cpuIndex] = min(16, max(1, t / (CPU_MAX_TEMP - CPU_MIN_TEMP)));
		_cpuIndex = (_cpuIndex + 1) % _cpuLineSize;
	}

	_registers[0] = ID_0;
	_registers[1] = ID_1;
	_registers[2] = ID_2;


	if (Button.GetDown(0x03))
		_cpuMode = !_cpuMode;

	u8g2.clearBuffer();

	u8g2.setColorIndex(1);

	char clocktext[6] = "00:00";
	setNumberToCharBuffer(_registers[REG_HOUR], 0, clocktext);
	setNumberToCharBuffer(_registers[REG_MINUTE], 3, clocktext);

	u8g2.setFont(u8g2_font_logisoso34_tn);
	u8g2.drawStr(0, 64 - (64 - 16 - 34) / 2, clocktext);

	drawSideTemp(CPU_LABEL, _registers[REG_CPU_TEMP], 25);
	drawSideTemp(GPU_LABEL, _registers[REG_GPU_TEMP], 49);

	u8g2.setFont(u8g2_font_rosencrantz_nbp_tr);
	u8g2.drawStr(0, 15, _cpuMode ? CPU_LABEL : GPU_LABEL);

	for (uint8_t i = 0; i < _cpuLineSize; i++)
	{
		uint8_t height = _cpuLine[(_cpuIndex + i) % _cpuLineSize];
		u8g2.drawBox(128 - (_cpuLineSize - i) * 5 + 1, 16 - height, 4, height);
	}

	u8g2.sendBuffer();
}

void drawSideTemp(const char* label, uint16_t value, uint8_t y)
{
	char temp[3] = "00";

	u8g2.setFont(u8g2_font_bitcasual_tu);
	u8g2.drawStr(100, y, label);

	setNumberToCharBuffer(value, 0, temp);
	u8g2.setFont(u8g2_font_timR12_tn);

	u8g2.drawStr(100, y + 14, temp);
	u8g2.drawCircle(125, y + 4, 2);
}

void setNumberToCharBuffer(uint16_t number, uint8_t offset, char* buffer)
{
	char buf[4];
	itoa(number, buf, 10);
	buffer[0 + offset] = number >= 10 ? buf[0] : '0';
	buffer[1 + offset] = number < 10 ? buf[0] : buf[1];
}

