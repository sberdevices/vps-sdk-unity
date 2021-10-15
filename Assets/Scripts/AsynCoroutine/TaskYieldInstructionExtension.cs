using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public static class TaskYieldInstructionExtension
    {
        public static TaskYieldInstruction AsCoroutine(this Task task)
        {
            if (task == null)
            {
                throw new NullReferenceException();
            }

            return new TaskYieldInstruction(task);
        }

        public static TaskYieldInstruction<T> AsCoroutine<T>(this Task<T> task)
        {
            if (task == null)
            {
                throw new NullReferenceException();
            }

            return new TaskYieldInstruction<T>(task);
        }
    }
}