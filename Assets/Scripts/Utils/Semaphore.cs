﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class Semaphore
    {
        private int counter;
        private int maxTaked;

        public Semaphore(int MaxTaked)
        {
            maxTaked = MaxTaked;
            counter = 0;
        }

        public void TakeOne()
        {
            if (counter < maxTaked)
                counter++;
        }

        public bool CheckState()
        {
            return counter < maxTaked;
        }

        public void Free()
        {
            if (counter > 0)
                counter--;
        }
    }
}
