﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Prototyping.Extensions
{
    public static class CollectionExtensions
    {
        public static string Slice(this string input, int? lower, int? upper, int? step)
        {
            return null;
        }

        public static string Slice(this string input, long? lower, long? upper, long? step)
        {
            return null;
        }

        public static List<T> Slice<T>(this IList<T> input, int? lower, int? upper, int? step)
        {
            return null;
        }

        public static string rstrip(this string input) => new string(input.ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray());

        public static void Deconstruct(this List<int> input, out int a, out int b)
        {
            a = input[0];
            b = input[1];
            //return (input[0], input[1]);
        }

        public static void Deconstruct(this (int, int)? input, out int a, out int b)
        {
            a = input.Value.Item1;
            b = input.Value.Item2;
            //return (input[0], input[1]);
        }

        public static bool isnumeric(this string s) => long.TryParse(s, out long result) | ulong.TryParse(s, out ulong uResult);

        public static T Pop<T>(this IList<T> list, int index)
        {
            var popped = list[index];
            list.RemoveAt(index);
            return popped;
        }

        public static T Pop<T>(this HashSet<T> set)
        {
            var first = set.First();
            set.Remove(first);
            return first;
        }

        public static IEnumerable<(int, T)> Enumerate<T>(this IEnumerable<T> input, int start = 0)
        {
            int i = start;
            foreach (var t in input)
                yield return (i++, t);
        }
    }
}
