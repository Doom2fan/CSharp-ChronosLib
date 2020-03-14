/*
 *  ChronosLib - A collection of useful things
 *  Copyright (C) 2018-2020 Chronos "phantombeta" Ouroboros
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Reflection;

namespace ChronosLib.Reflection {
    public struct PropertyDelegates<T> {
        public PropertyInfo Info { get; set; }
        public string Name { get; set; }

        public Func<object, T> Getter { get; set; }
        public Action<object, T> Setter { get; set; }
    }

    /// <summary>A class that implements extension methods for reflection.</summary>
    public static class ReflectionExtensions {
#pragma warning disable IDE0051 // Remove unused private members
        private static Func<object, V> GetDelegateHelper<T, U, V> (PropertyInfo prop)
            where U : V {
            var del = (Func<T, U>) prop.GetMethod.CreateDelegate (typeof (Func<T, U>), null);
            return new Func <object, V> (self => {
                return (V) del ((T) self);
            });
        }

        private static Action<object, V> SetDelegateHelper<T, U, V> (PropertyInfo prop)
            where U : V {
            var del = (Action<T, U>) prop.SetMethod.CreateDelegate (typeof (Action<T, U>), null);
            return new Action<object, V> ((self, val) => {
                del ((T) self, (U) val);
            });
        }
#pragma warning restore IDE0051 // Remove unused private members

        /// <summary>Creates delegates for a property's getter and setter.</summary>
        /// <param name="prop">The property to create the delegates for.</param>
        /// <param name="createGetter">Whether to create a getter.</param>
        /// <param name="createSetter">Whether to create a setter.</param>
        /// <returns>An instance of EchelonPropertyDelegates.</returns>
        public static PropertyDelegates<T> CreateSetGetDelegates<T> (this PropertyInfo prop, bool createGetter, bool createSetter) {
            if (!createGetter && !createSetter)
                throw new ArgumentException ("At least one of createGetter and createSetter must be true");

            var declType = prop.DeclaringType;

            var ret = new PropertyDelegates<T> () {
                Info = prop,
                Name = prop.Name
            };

            // Create the getter
            if (createGetter && prop.GetMethod != null) {
                var getterHelperInfo = typeof (ReflectionExtensions).GetMethod ("GetDelegateHelper", BindingFlags.Static | BindingFlags.NonPublic);
                var getterHelper = getterHelperInfo.MakeGenericMethod (new Type [] { declType, prop.PropertyType, typeof (T) });

                ret.Getter = (Func<object, T>) getterHelper.Invoke (null, new object [] { prop });
            }

            // Create the setter
            if (createSetter && prop.SetMethod != null) {
                var setterHelperInfo = typeof (ReflectionExtensions).GetMethod ("SetDelegateHelper", BindingFlags.Static | BindingFlags.NonPublic);
                var setterHelper = setterHelperInfo.MakeGenericMethod (new Type [] { declType, prop.PropertyType, typeof (T) });

                ret.Setter = (Action<object, T>) setterHelper.Invoke (null, new object [] { prop });
            }

            return ret;
        }
    }
}
