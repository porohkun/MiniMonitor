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
//#include "src\Digits.h"
//#include "tall.h"

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
#define CPU_MIN_TEMP 20
#define CPU_MAX_TEMP 60
#define GPU_MIN_TEMP 20
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
uint8_t _cpuLine[27]{ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO};
uint8_t _gpuLine[27]{ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO, ZERO};
uint8_t _lineSize = sizeof(_cpuLine) / sizeof(_cpuLine[0]);
uint8_t _lineIndex = 0;

const uint8_t tall[209] U8G2_FONT_SECTION("tall") =
	"\20\0\3\2\3\4\2\4\4\5\14\0\371\5\0\5\371\0\0\0\0\0\271 \5\0\321\1\60\12e\303"
	"\263d\376[\262\0\61\13e\303\225II\330\237\6\1\62\13e\303\263dao\35\7\1\63\15e\303"
	"\61h\265\65\354\250%\13\0\64\20e\303\27f\246$JJI\224\14ZX\1\65\15e\303q\14\207"
	"\64\354\250%\13\0\66\15e\303\263d\342\220d~K\26\0\67\15e\303\61\210\265\260\26\326\302\32\0"
	"\70\15e\303\263dZ\262d~K\26\0\71\15e\303\263d~K\206PK\26\0:\10\262\311\61\204"
	"C\0C\13e\303\263db\177K\26\0G\14e\303\263db\237\66-Y\0P\14e\303\61$\231"
	"\337\6%,\2U\11e\303\221\371\337\222\5\0\0\0\4\377\377\0";
const uint8_t tall_s[147] U8G2_FONT_SECTION("tall") =
	"\4\0\3\2\3\3\1\2\4\5\7\0\376\5\376\5\376\0\0\0\0\0)C\12}<K&\266%\13"
	"\0G\12}<K&\226\266d\1P\13}\34C\222\331\6%\14\1U\10}\34\231o\311\2\0\0"
	"\0\4\377\377\0";
const uint8_t high[913] U8G2_FONT_SECTION("tall") = 
  "\13\0\5\2\5\6\1\5\6\23/\0\0/\0\0\0\0\0\0\0\3t\60d\363\15q\262p(\241"
  "\207\26\356\256\15$\244%\14\244m\256\15$\244%\14\244m\256\15$\244%\14\244m\256\15$\244%"
  "\14\244m\256\15$\244%\14\244m\256\15$\244%\14\244m\256\15$\244%\14\244m\256\15$\244%"
  "\14\244m\256\15$\244%\14\244m\256\15$\244%\14\244\355.\34J\350\241\205#\0\61I\363\15q"
  "\64\230\20\270\17\260\60\226\320\330\302\366\312PBJB\320\312>\310`B\340>\310`B\340>\310`"
  "B\340>\310`B\340>\310`B\340>\310`B\340>\310`B\340>\310`B\340\366\302\7\22\372"
  "\13\3\11\37\62K\363\15q\262p(\241\207\26\356\256\15$\244%\14\244\355\203\14&\4\356\203\14&"
  "\4\356\203\14&\4\356\203\14&\4\356\3\14&\4\356\3\14&\4\356\3\14&\4\356\3\14&\4\356"
  "\203\14&\4\356\203,| \241\277\60\220\360\1\63M\363\15q\260\360\201\204\376\302@\302\367\1\6\23"
  "\2\367\1\6\23\2\367\1\26\16%\364\320\302}\240\301\204\300}\220\301\204\300}\220\301\204\300}\220\301"
  "\204\300}\220\301\204\300}\220\301\204\300\315\265\201\204\264\204\201\264\335\205C\11=\264p\4\64U\363\15"
  "q\66\230\20\270\17\62\230\20\270\17\260\60\226\320\330\302>\300\302XBc\13\333+C\11)\11A+"
  "\333+C\11)\11A+\273K#\11I\11!K\273K#\11I\11!K\273\13\37H\350/\14$"
  "|\37`\60!p\37d\60!p\37d\60!p\4\65M\363\15q\260\360\201\204\376\302@\302\67\7"
  "\23\2\367A\6\23\2\367A\26\36I\350G\26\336\7\32L\10\334\7\31L\10\334\7\31L\10\334\7"
  "\31L\10\334\7\31L\10\334\7\31L\10\334\134\33HHK\30H\333]\70\224\320C\13G\0\66`"
  "\363\15q\262p(\241\207\26\356\256\15$\244%\14\244m\16&\4\356\203,<\222\320\217,\274\273\66"
  "\220\220\226\60\220\266\271\66\220\220\226\60\220\266\271\66\220\220\226\60\220\266\271\66\220\220\226\60\220\266\271\66"
  "\220\220\226\60\220\266\271\66\220\220\226\60\220\266\271\66\220\220\226\60\220\266\273p(\241\207\26\216\0\67F"
  "\363\15q\260\360\201\204\376\302@\302\367A\6\23\2\367A\6\23\2\367\1\6\23\2\367A\6\23\2\367"
  "A\6\23\2\367\1\6\23\2\367A\6\23\2\367A\6\23\2\367\1\6\23\2\367A\6\23\2\367A\6"
  "\23\2\307\0\70c\363\15q\262p(\241\207\26\356\256\15$\244%\14\244m\256\15$\244%\14\244\355"
  ".\34J\350\241\205\273k\3\11i\11\3i\233k\3\11i\11\3i\233k\3\11i\11\3i\233k"
  "\3\11i\11\3i\233k\3\11i\11\3i\233k\3\11i\11\3i\233k\3\11i\11\3i\273\13"
  "\207\22zh\341\10\0\71`\363\15q\262p(\241\207\26\356\256\15$\244%\14\244m\256\15$\244%"
  "\14\244m\256\15$\244%\14\244m\256\15$\244%\14\244m\256\15$\244%\14\244m\256\15$\244%"
  "\14\244m\256\15$\244%\14\244\355.<\222\320\217,\274\17\62\230\20\270\271\66\220\220\226\60\220\266\273"
  "p(\241\207\26\216\0:\42\347\312Y\260\60\220Pa aga \241\302@\302>\205\3\11\25\6"
  "\22v\26\6\22*\14$\14\0\0\0\4\377\377\0";

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
	u8g2.setFont(tall);

	u8g2.setFont(tall);
	char temp[6] = "00000";
	numberToCharBuffer(_registers[REG_CPU_TEMP], temp, 3, 5, ' ');
	u8g2.drawStr(106, 32, temp);
	u8g2.drawFrame(124, 27, 4, 4);
	numberToCharBuffer(_registers[REG_GPU_TEMP], temp, 3, 5, ' ');
	u8g2.drawStr(106, 57, temp);
	u8g2.drawFrame(124, 52, 4, 4);

	u8g2.drawStr(1, 7, _cpuMode ? CPU_LABEL : GPU_LABEL);
	u8g2.setFont(tall_s);
	u8g2.drawStr(108, 22, CPU_LABEL);
	u8g2.drawStr(108, 47, GPU_LABEL);

	u8g2.setFont(high);
	numberToCharBuffer(_registers[REG_MINUTE], temp, 5, 5, '0');
	numberToCharBuffer(_registers[REG_HOUR], temp, 2, 2, '0');
	temp[2]=':';

	u8g2.drawStr(0, 64, temp);

	for (uint8_t i = 0; i < _lineSize; i++)
	{
		uint8_t height = max(1, (_cpuMode ? _cpuLine : _gpuLine)[(_lineIndex + i) % _lineSize]);
		u8g2.drawBox(128 - (_lineSize - i) * 4 + 1, 16 - height, 3, height);
	}

	u8g2.sendBuffer();
}

void numberToCharBuffer(uint8_t number, char *buffer, uint8_t length, uint8_t bufferLength, char zeroChar)
{
	for (int8_t i = length - 1; i >= 0; i--)
	{
		uint8_t digit = number % 10;
		number = number / 10;
		buffer[i] = (digit == 0 && i < length - 1) ? zeroChar : (digit + '0');
	}
	for (uint8_t i = length; i < bufferLength - 1; i++)
		buffer[i] = '\0';
}

