/*
 *  GZDoomLib - A library for using GZDoom's file formats in C#
 *  Copyright (C) 2018-2019 Chronos "phantombeta" Ouroboros
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

using GZDoomLib.UDMF.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GZDoomLib.UDMF {
    [AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class UDMFDataAttribute : Attribute {
        private string identifier;

        public UDMFDataAttribute (string name) {
            identifier = name;
        }

        public virtual string Identifier { get => identifier; }
    }

    public class UDMFParser {
        protected UDMFParser_Internal parser;

        public UDMFParser () {
            parser = new UDMFParser_Internal (new UDMFScanner ());
        }

        public long Parse (string input) {
            if (input is null)
                throw new ArgumentNullException (nameof (input));

            var sw = new System.Diagnostics.Stopwatch ();
            sw.Restart ();
            var data = parser.Parse (input);
            long time = sw.ElapsedTicks;
            sw.Stop ();

            foreach (var error in parser.Errors)
                Console.WriteLine ($"Message: \"{error.Message}\", Line: \"{error.Line}\", Column: {error.Column}");

            //Console.WriteLine ($"Vertices: {data.Vertices.Count}, Linedefs: {data.Linedefs.Count}, Sidedefs: {data.Sidedefs.Count}, Sectors: {data.Sectors.Count}, Things: {data.Things.Count}");

            return time;
        }
    }
}
