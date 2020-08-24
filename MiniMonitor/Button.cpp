#include "Arduino.h"
#include "Button.h"



void ButtonClass::SetButtons(byte pins[])
{
	for (byte i = 0; i < _length; i++)
	{
		_pins[i] = pins[i];
		pinMode(_pins[i], INPUT_PULLUP);
	}
}

void ButtonClass::Loop()
{
	unsigned long time = millis();

	for (byte i = 0; i < _length; i++)
	{
		bool state = digitalRead(_pins[i]) == LOW;
		_prevStates[i] = _states[i];
		if (_isStateChanging[i])
		{
			if (time - _beginStateTime[i] >= _precision[i])
			{
				_isStateChanging[i] = false;
				if (state == _nextStates[i])
					_states[i] = state;
			}
		}
		else
		{
			if (state != _states[i])
			{
				_isStateChanging[i] = true;
				_nextStates[i] = state;
				_beginStateTime[i] = time;
			}
		}
	}
}

bool ButtonClass::GetDown(byte pin)
{
	byte i = this->GetIndex(pin);
	if (i == 255)
		return false;
	return _states[i] && !_prevStates[i];
}

bool ButtonClass::GetUp(byte pin)
{
	byte i = this->GetIndex(pin);
	if (i == 255)
		return false;
	return !_states[i] && _prevStates[i];
}

bool ButtonClass::GetState(byte pin)
{
	byte i = this->GetIndex(pin);
	if (i == 255)
		return false;
	return _states[i];
}

byte ButtonClass::GetIndex(byte pin)
{
	for (byte i = 0; i < _length; i++)
		if (_pins[i] == pin)
			return i;
	return 255;
}

ButtonClass Button;