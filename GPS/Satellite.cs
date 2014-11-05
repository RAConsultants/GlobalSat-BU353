using System;
using System.Collections.Generic;

namespace GPS
{
	public class Satellite
	{
		public int Id;
		public bool Used;		
		public int PRN;
		public int SignalQuality;
		public int Azimuth;
		public int Elevation;
		public object Thing;
	}

    public class Satellites : Dictionary<int, Satellite>
    {
        public void Add(Satellite sat)
        {
            if (this.ContainsKey(sat.Id))
            {
                Satellite existingSat = this[sat.Id];
                existingSat.Azimuth = sat.Azimuth;
                existingSat.Elevation = sat.Elevation;
                existingSat.PRN = sat.PRN;
                existingSat.SignalQuality = sat.SignalQuality;
                existingSat.Used = sat.Used;
            }
            else
            {
                this.Add(sat.Id, sat);
            }
        }
    }
}