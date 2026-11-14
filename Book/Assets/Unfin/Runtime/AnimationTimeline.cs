using System;
using UnityEngine;

namespace Unfin
{
    // Immutable per-clip timing shared by player and enemy sprite playback.
    public sealed class AnimationTimeline
    {
        readonly float[] ends;
        public float Duration { get; }
        public static AnimationTimeline Get(ref AnimationTimeline cache,ref float[] source,float[] durations)
        {
            if(cache==null||!ReferenceEquals(source,durations))
            {
                source=durations;
                cache=new AnimationTimeline(durations.Length,i=>durations[i]);
            }
            return cache;
        }
        public AnimationTimeline(int count, Func<int, float> duration)
        {
            if (count < 1) throw new ArgumentException("An animation needs a frame.");
            ends = new float[count];
            float total = 0;
            for (int i = 0; i < count; i++)
            {
                float value = duration(i);
                if (value < 0 || float.IsNaN(value) || float.IsInfinity(value))
                    throw new ArgumentException("Invalid frame duration.");
                ends[i] = total += value;
            }
            Duration = total;
        }
        public int FrameAt(float time, bool loop)
        {
            time = loop && Duration > 0 ? Mathf.Repeat(time, Duration) : Mathf.Max(0, time);
            int low = 0, high = ends.Length - 1;
            while (low < high)
            {
                int mid = (low + high) / 2;
                if (time < ends[mid]) high = mid; else low = mid + 1;
            }
            return low;
        }
    }
}
