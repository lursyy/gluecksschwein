using System;
using System.Collections.Generic;

public static class Extensions
{
    private static readonly Random Rng = new Random();  

    public static void Shuffle<T>(this IList<T> list)  
    {
        int n = list.Count;  
        while (n > 1) {  
            n--;  
            int k = Rng.Next(n + 1);  
            T value = list[k];  
            list[k] = list[n];  
            list[n] = value;  
        }  
    }

    public static T CycleNext<T>(this IList<T> list, T current) => list.Cycle(current, true);
    
    public static T CyclePrev<T>(this IList<T> list, T current) => list.Cycle(current, false);

    private static T Cycle<T>(this IList<T> list, T current, bool positive)
    {
        int elemIndex = list.IndexOf(current);
        if (elemIndex == -1)
        {
            throw new InvalidOperationException($"{nameof(current)} not in list");
        }
        int amount = positive ? 1 : -1;
        return list[(elemIndex + list.Count + amount) % list.Count];
    }
}