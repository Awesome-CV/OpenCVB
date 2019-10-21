﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cv = OpenCvSharp;
using System.Drawing;
namespace CS_Classes
{
    public class CS_BlurGaussian
    {
        public void New() {}
        public void Run(cv.Mat color, cv.Mat result1, int kernelSize)
        {
            if (kernelSize % 2 == 0) kernelSize -= 1; // kernel size must be odd
            cv.Cv2.GaussianBlur(color, result1, new cv.Size(kernelSize, kernelSize), 0, 0);
        } 
    }
    public class CS_BlurMedian 
    {
        public void New(){}
        public void Run(cv.Mat color, cv.Mat result1, int kernelSize)
        {
            if (kernelSize % 2 == 0) kernelSize -= 1; // kernel size must be odd
            cv.Cv2.GaussianBlur(color, result1, new cv.Size(kernelSize, kernelSize), 0, 0);
        }
    }

}