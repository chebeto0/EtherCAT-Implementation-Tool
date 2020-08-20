import time


def main():
	DriveSequence()
	output.println('')
	output.println('Finished Drive Sequence')
	

def DriveSequence():
	target_positions = [1000,5000,-3000]
	velocity = 100
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
		output.println('Target Reached')


def SetNewPosition(velocity=200, acceleration=100, deceleration=100, position=0):
	device.SetControlWordPdo(device.CurrentCW, 0x0000)
	time.sleep(0.1)
	device.SetTargetPosition(position)
	device.SetProfileVelocity(velocity)
	device.SetProfileAcceleration(acceleration)
	device.SetProfileDeceleration(deceleration)
	time.sleep(0.1)
	device.SetControlWordPdo(device.CurrentCW, 0x0030)


if __name__ == '__builtin__':
	main()