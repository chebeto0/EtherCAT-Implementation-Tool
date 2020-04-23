import time

def main():
	DriveSequence()
	#StepperMode()
	print('Finished XXX')

def DriveSequence():
	target_positions = [10000,50000,-30000]
	velocity = 1000
	acceleration = 100
	deceleration = 100
	device.SetOperModePPM()
	device.SetControlWordPdo(0x06, 0x0000)
	time.sleep(0.1)
	device.SetControlWordPdo(0x0F, 0x0000)
	time.sleep(0.1)
	for position in target_positions:
		SetNewPosition(velocity, acceleration, deceleration, position)
		time.sleep(0.1)
		while not device.stateMachineDsp402.TargetReached:
			pass
		print('Target Reached')

def SetNewPosition(velocity=200, acceleration=100, deceleration=100, position=0):
	device.SetControlWordPdo(device.CurrentCW, 0x0000)
	time.sleep(0.1)
	device.SetTargetPosition(position)
	device.SetProfileVelocity(velocity)
	device.SetProfileAcceleration(acceleration)
	device.SetProfileDeceleration(deceleration)
	time.sleep(0.1)
	device.SetControlWordPdo(device.CurrentCW, 0x0030)

def StepperMode(mode=0, angle=30, amplitude=75, target_duration = 500, pairs_of_poles = 3):

    offset = list()

    device.SdoWrite(0x2800, 0x01, mode)
    device.SdoWrite(0x2800, 0x02, angle)
    device.SdoWrite(0x2800, 0x03, amplitude)
    device.SdoWrite(0x2800, 0x04, target_duration)
    time.sleep(0.1)
    device.SetControlWord(0x06,0)
    time.sleep(0.1)
    device.SetControlWord(0x07,0)
    time.sleep(0.1)
    device.SetControlWord(0x0f,0)
    time.sleep(0.1)
    device.SetOperModeStepper()

    num_steps = int(round(360/abs(angle)*pairs_of_poles,0))

    for i in range(0,num_steps):
        time.sleep(target_duration/1000.0)
        device.SdoRead(0x2800, 0x07)
        temp = device.ObjectDictionary.GetItem(0x2800,0x07).Value
        offset.append(temp)
        print( temp )

    device.SetControlWord(0,0)

    print('')
    print(sum(offset)/len(offset))

if __name__ == '__builtin__':
	main()