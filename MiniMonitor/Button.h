#include "Arduino.h"

#define BTN_COUNT 5

class ButtonClass
{
public:
	void SetButtons(byte pins[]);
	void Loop();
	bool GetDown(byte pin);
	bool GetUp(byte pin);
	bool GetState(byte pin);

private:
	byte GetIndex(byte pin);
	const byte _length = BTN_COUNT;
	byte _pins[BTN_COUNT];
	bool _states[BTN_COUNT];
	bool _prevStates[BTN_COUNT];
	bool _nextStates[BTN_COUNT];
	bool _isStateChanging[BTN_COUNT];
	int _precision[BTN_COUNT] = { 40, 40, 40, 40, 40 };
	unsigned long _beginStateTime[BTN_COUNT];
};

extern ButtonClass Button;
