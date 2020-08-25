#include "Arduino.h"

class TimerClass
{
public:
	bool IsRaised;
	void SetInterval(byte interval) { _interval = interval; }
	void Loop();
	void Start();
	void Stop();

private:
	byte _interval;
	bool _enabled;
	unsigned long _lastTick;
};

extern TimerClass Timer;

