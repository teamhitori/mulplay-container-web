﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamHitori.Mulplay.Container.Web.Components
{
    public static class FunctionalExtensions
    {
        public static T DoIfNull<T>(this T obj, Func<T> action)
        {
            if(obj == null)
            {
                return action();
            }

            return obj;
        }

        public static IEnumerable<T> Upsert<T>(this IEnumerable<T> collection, T itemIn, Func<T, bool> pred)
        {
            var updated = false;
            foreach (var item in collection)
            {
                if (pred(item))
                {
                    updated = true;
                    yield return itemIn;
                } else
                {
                    yield return item;
                }
            }

            if(!updated)
            {
                yield return itemIn;
            }
        }

        public static IEnumerable<T> Remove<T>(this IEnumerable<T> collection,Func<T, bool> pred)
        {
            foreach (var item in collection)
            {
                if (pred(item))
                {

                }
                else
                {
                    yield return item;
                }
            }
        }
    }
}