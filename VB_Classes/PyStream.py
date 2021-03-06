import argparse
import mmap
import array
import cv2 as cv
import numpy as np
import os, time, sys
from time import sleep
import ctypes
def Mbox(title, text, style):
    return ctypes.windll.user32.MessageBoxW(0, text, title, style)

def PyStreamRun(OpenCVCode, scriptName):
    parser = argparse.ArgumentParser(description='Pass in length of MemMap region.', formatter_class=argparse.ArgumentDefaultsHelpFormatter)
    parser.add_argument('--MemMapLength', type=int, default=0, help='The number of bytes are in the memory mapped file.')
    parser.add_argument('--pipeName', default='', help='The name of the input pipe for image data.')
    args = parser.parse_args()

    # this apparently unnecessary code is quite useful!  When python script is invoked directly, it starts OpenCVB.exe.
    MemMapLength = args.MemMapLength
    if MemMapLength == 0:
        MemMapLength = 400 # these values have been generously padded (on both sides) but if they grow...
        args.pipeName = 'OpenCVBImages0' # we always start with 0 and since it is only invoked once, 0 is all it will ever be.
        ocvb = os.getcwd() + '/../bin/Debug/OpenCVB.exe'
        if os.path.exists(ocvb):
            tupleArg = (' ', scriptName)
            pid = os.spawnv(os.P_NOWAIT, ocvb, tupleArg) # OpenCVB.exe will be run with this .py script

    pipeName = '\\\\.\\pipe\\' + args.pipeName
    while True:
        try:
            pipeIn = open(pipeName, 'rb')
            break
        except Exception as exception:
            time.sleep(0.1) # sleep for a bit to wait for OpenCVB to start...
    try: 
        mm = mmap.mmap(0, MemMapLength, tagname='Python_MemMap')
        frameCount = -1
        while True:
            mm.seek(0)
            arrayDoubles = array.array('d', mm.read(MemMapLength))
            rgbBufferSize = int(arrayDoubles[1])
            depthBufferSize = int(arrayDoubles[2])
            rows = int(arrayDoubles[3])
            cols = int(arrayDoubles[4])

            if rows > 0:
                if arrayDoubles[0] == frameCount:
                    sleep(0.001)
                else:
                    frameCount = arrayDoubles[0] 
                    rgb = pipeIn.read(int(rgbBufferSize))
                    depthData = pipeIn.read(int(depthBufferSize))
                    depthSize = rows, cols, 1
                    try:
                        depth = np.array(np.frombuffer(depthData, np.float32).reshape(depthSize))
                    except:
                        print("unable to reshape the depth data")
                        sys.exit()
                    depth_colormap = cv.applyColorMap(cv.convertScaleAbs(depth, alpha=0.03), cv.COLORMAP_HSV)
                    rgbSize = rows, cols, 3
                    try:
                        imgRGB = np.array(np.frombuffer(rgb, np.uint8).reshape(rgbSize))
                    except:
                        print("Unable to reshape the RGB data")
                        sys.exit()
                    OpenCVCode(imgRGB, depth_colormap, frameCount)
                    cv.waitKey(1)
                    
    except Exception as exception:
        print(exception)
        Mbox('PyStream.py', 'Failure - see console output', 1)    