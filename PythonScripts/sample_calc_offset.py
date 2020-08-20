import time

def main():
	CalcOffset()
	output.println('')
	output.println('Finished Calc Offset')
	

def CalcOffset(mode=0, angle=30, amplitude=75, target_duration = 500, pairs_of_poles = 3):

    output.println('Calculating offset in stepper mode')
    output.println('')

    offset = list()

    device.SdoWrite(0x2800, 0x01, mode)
    device.SdoWrite(0x2800, 0x02, angle)
    device.SdoWrite(0x2800, 0x03, amplitude)
    device.SdoWrite(0x2800, 0x04, target_duration)
    time.sleep(0.1)
    device.SetControlWordPdo(0x06,0)
    time.sleep(0.1)
    device.SetControlWordPdo(0x07,0)
    time.sleep(0.1)
    device.SetControlWordPdo(0x0f,0)
    time.sleep(0.1)
    device.SetOperModeStepper()

    num_steps = int(round(360/abs(angle)*pairs_of_poles,0)) #calculate numeber of steps to ensure a full rotor revolution

    for i in range(0,num_steps):
        time.sleep(target_duration/1000.0)
        device.SdoRead(0x2800, 0x07)
        temp = device.ObjectDictionary.GetItem(0x2800,0x07).Value
        offset.append(temp)
        output.println( temp )

    device.SetControlWordPdo(0,0)
    
    mean_offset = sum(offset)/len(offset)

    output.println('')
    output.println('Offset: {moffset}'.format(moffset = mean_offset))


if __name__ == '__builtin__':
	main()
