#include "Arduino.h"
#include "Timer.h"



void TimerClass::Loop()
{
	if (_enabled)
	{
		unsigned long time = millis();
		if (time - _lastTick >= _interval)
		{
			_lastTick = time;
			IsRaised = true;
		}
		else
			IsRaised = false;
	}
}

void TimerClass::Start()
{
	_enabled = true;
	_lastTick = millis();
}

void TimerClass::Stop()
{
	_enabled = false;
	IsRaised = false;
}

TimerClass Timer;