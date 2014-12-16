﻿/*
    Copyright 2014 Travel Modelling Group, Department of Civil Engineering, University of Toronto

    This file is part of XTMF.

    XTMF is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    XTMF is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with XTMF.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TMG.Emme;
using XTMF;
using TMG;
namespace Tasha.Utilities
{

    public class BasicTravelDemandModel : ITravelDemandModel
    {
        [RunParameter("Input Base Directory", "../../Input", "The directory to use for getting input.")]
        public string InputBaseDirectory { get;set; }

        public string Name { get; set; }

        public IList<INetworkData> NetworkData { get;set; }

        public string OutputBaseDirectory { get;set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        public IZoneSystem ZoneSystem { get;set; }

        [SubModelInformation(Required = false, Description = "Children to execute")]
        public ISelfContainedModule[] ToExecute;

        public bool ExitRequest()
        {
            return false;
        }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }

        public void Start()
        {
            for(int i = 0; i < ToExecute.Length; i++)
            {
                ToExecute[i].Start();
            }
        }
    }

}
