﻿/*
    Copyright 2015 Travel Modelling Group, Department of Civil Engineering, University of Toronto

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
using System.Text;
using TMG.Emme;
using XTMF;
using Tasha.Common;
using TMG;
using Datastructure;
using TMG.Input;
using System.Threading;
using Tasha.XTMFModeChoice;

namespace Tasha.EMME
{
    public sealed class CreateHOVEmmeBinaryMatrix : IPostHouseholdIteration
    {
        [RootModule]
        public ITravelDemandModel Root;
        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get; set; }

        private float[][][] Matrix;

        [SubModelInformation(Required = true, Description = "The location to save the SOV matrix.")]
        public FileLocation SOVMatrixSaveLocation;

        [SubModelInformation(Required = true, Description = "The location to save the HOV matrix.")]
        public FileLocation HOVMatrixSaveLocation;

        [RunParameter("Start Time", "6:00AM", typeof(Time), "The start of the time to record.")]
        public Time StartTime;

        [RunParameter("End Time", "9:00AM", typeof(Time), "The end of the time to record (non inclusive).")]
        public Time EndTime;

        private SpinLock WriteLock = new SpinLock(false);

        public void HouseholdIterationComplete(ITashaHousehold household, int hhldIteration, int totalHouseholdIterations)
        {
            var persons = household.Persons;
            for(int i = 0; i < persons.Length; i++)
            {
                var expFactor = persons[i].ExpansionFactor;
                var tripChains = persons[i].TripChains;
                
                for(int j = 0; j < tripChains.Count; j++)
                {
                    if((tripChains[j].EndTime < StartTime) | (tripChains[j].StartTime) > EndTime)
                    {
                        continue;
                    }
                    var jointTour = tripChains[j].JointTrip && tripChains[j].JointTripRep;
                    var trips = tripChains[j].Trips;
                    // check to see if we should be running access or egress for this person on their trip chain
                    bool access = true;
                    for(int k = 0; k < trips.Count; k++)
                    {
                        var startTime = trips[k].TripStartTime;
                        if(startTime >= StartTime & startTime < EndTime)
                        {
                            var modeChosen = trips[k].Mode;
                            int accessModeIndex = -1;
                            if(Passenger.Mode == modeChosen)
                            {

                                var driversTrip = trips[k]["Driver"] as ITrip;
                                // driver originData
                                var driverOrigin = ZoneSystem.GetFlatIndex(driversTrip.OriginalZone.ZoneNumber);
                                var passengerOrigin = ZoneSystem.GetFlatIndex(trips[k].OriginalZone.ZoneNumber);
                                var passengerDestination = ZoneSystem.GetFlatIndex(trips[k].DestinationZone.ZoneNumber);
                                var driverDestination = ZoneSystem.GetFlatIndex(driversTrip.DestinationZone.ZoneNumber);
                                
                                var driverTripChain = driversTrip.TripChain;
                                var driverOnJoint = driverTripChain.JointTrip;
                                float driverExpansionFactor = driverTripChain.Person.ExpansionFactor;
                                // subtract out the old data
                                AddToMatrix(-driverExpansionFactor, driverOnJoint, driverOrigin, driverDestination);
                                // add in our 3 trip leg data
                                if(driverOrigin != passengerOrigin)
                                {
                                    // this really is driver on joint
                                    AddToMatrix(driverExpansionFactor, driverOnJoint, driverOrigin, passengerOrigin);
                                }
                                AddToMatrix(driverExpansionFactor, true, passengerOrigin, passengerDestination);
                                if(passengerDestination != driverDestination)
                                {
                                    AddToMatrix(driverExpansionFactor, driverOnJoint, passengerDestination, driverDestination);
                                }
                            }
                            else if((accessModeIndex = UsesAccessMode(modeChosen)) >= 0)
                            {
                                if(AccessModes[accessModeIndex].GetTranslatedOD(tripChains[j], trips[k], access, out var origin, out var destination))
                                {
                                    var originIndex = ZoneSystem.GetFlatIndex(origin.ZoneNumber);
                                    var destinationIndex = ZoneSystem.GetFlatIndex(destination.ZoneNumber);
                                    WriteLock.Enter(ref (bool gotLock = false));
                                    Matrix[0][originIndex][destinationIndex] += expFactor;
                                    if(gotLock) WriteLock.Exit(true);
                                }
                                access = false;
                            }
                            else
                            {
                                var originIndex = ZoneSystem.GetFlatIndex(trips[k].OriginalZone.ZoneNumber);
                                var destinationIndex = ZoneSystem.GetFlatIndex(trips[k].DestinationZone.ZoneNumber);
                                AddToMatrix(expFactor, jointTour, originIndex, destinationIndex);
                            }
                        }
                    }
                }
            }
        }

        private void AddToMatrix(float expFactor, bool jointTrip, int originIndex, int destinationIndex)
        {
            var row = Matrix[jointTrip ? 1 : 0][originIndex];
            WriteLock.Enter(ref (bool gotLock = false));
            row[destinationIndex] += expFactor;
            if(gotLock) WriteLock.Exit(true);
        }

        public sealed class ModeLink : IModule
        {
            [RootModule]
            public ITashaRuntime Root;

            [RunParameter("Mode Name", "Auto", "The name of the mode")]
            public string ModeName;

            internal ITashaMode Mode;

            public string Name { get; set; }

            public float Progress { get; set; }

            public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

            public bool RuntimeValidation(ref string error)
            {
                foreach(var mode in Root.AllModes)
                {
                    if(mode.ModeName == ModeName)
                    {
                        Mode = mode;
                        return true;
                    }
                }
                error = "In '" + Name + "' we were unable to find a mode called '" + ModeName + "'";
                return false;
            }
        }

        public sealed class AccessModeLink : IModule
        {
            [RootModule]
            public ITashaRuntime Root;

            [RunParameter("Mode Name", "DAT", "The name of the mode")]
            public string ModeName;

            [RunParameter("Count Access", true, "True to count for access, false to count for egress.")]
            public bool CountAccess;

            [RunParameter("Access Tag Name", "AccessStation", "The tag used for storing the zone used for access.")]
            public string AccessZoneTagName;

            internal ITashaMode Mode;

            public string Name { get; set; }

            public float Progress { get; set; }

            public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

            public bool GetTranslatedOD(ITripChain chain, ITrip trip, bool access, out IZone origin, out IZone destination)
            {
                if(CountAccess ^ (!access))
                {
                    origin = trip.OriginalZone;
                    destination = chain[AccessZoneTagName] as IZone;
                    return destination != null;
                }
                else
                {
                    origin = chain[AccessZoneTagName] as IZone;
                    destination = trip.DestinationZone;
                    return origin != null;
                }
            }

            public bool RuntimeValidation(ref string error)
            {
                foreach(var mode in Root.AllModes)
                {
                    if(mode.ModeName == ModeName)
                    {
                        Mode = mode;
                        return true;
                    }
                }
                error = "In '" + Name + "' we were unable to find a mode called '" + ModeName + "'";
                return false;
            }
        }

        [SubModelInformation(Required = false, Description = "The modes to listen for.")]
        public ModeLink[] Modes;

        [SubModelInformation(Required = true, Description = "The link to the passenger mode.")]
        public ModeLink Passenger;

        [SubModelInformation(Required = false, Description = "The access modes to listen for.")]
        public AccessModeLink[] AccessModes;

        /// <summary>
        /// check to see if the mode being used for this trip is one that we are interested in.
        /// </summary>
        /// <param name="trip"></param>
        /// <returns></returns>
        private bool UsesMode(ITashaMode mode)
        {
            for(int i = 0; i < Modes.Length; i++)
            {
                if(Modes[i].Mode == mode)
                {
                    return true;
                }
            }
            return false;
        }

        private int UsesAccessMode(ITashaMode mode)
        {
            for(int i = 0; i < AccessModes.Length; i++)
            {
                if(AccessModes[i].Mode == mode)
                {
                    return i;
                }
            }
            return -1;
        }

        private SparseArray<IZone> ZoneSystem;
        private int NumberOfZones;

        public void IterationStarting(int iteration, int totalIterations)
        {
            // get the newest zone system
            ZoneSystem = Root.ZoneSystem.ZoneArray;
            NumberOfZones = ZoneSystem.Count;
            if(Matrix == null)
            {
                Matrix = new float[2][][];
                for(int j = 0; j < 2; j++)
                {
                    Matrix[j] = new float[NumberOfZones][];
                    for(int i = 0; i < Matrix.Length; i++)
                    {
                        Matrix[j][i] = new float[NumberOfZones];
                    }
                }
            }
            else
            {
                for(int j = 0; j < Matrix.Length; j++)
                {
                    // clear out old trips
                    for(int i = 0; i < Matrix[j].Length; i++)
                    {
                        Array.Clear(Matrix[j][i], 0, Matrix[j][i].Length);
                    }
                }
            }
        }

        public IModeAggregationTally[] SpecialGenerators;

        public void IterationFinished(int iteration, int totalIterations)
        {
            // Apply the special generators
            for(int i = 0; i < SpecialGenerators.Length; i++)
            {
                SpecialGenerators[i].IncludeTally(Matrix[0]);
            }
            // write to disk
            new EmmeMatrix(ZoneSystem, Matrix[0]).Save(SOVMatrixSaveLocation, true);
            new EmmeMatrix(ZoneSystem, Matrix[1]).Save(HOVMatrixSaveLocation, true);
        }

        public void Load(int maxIterations)
        {

        }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }

        public void HouseholdComplete(ITashaHousehold household, bool success)
        {

        }

        public void HouseholdStart(ITashaHousehold household, int householdIterations)
        {

        }
    }
}