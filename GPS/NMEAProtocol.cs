using System;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;

using System.IO;

namespace GPS
{
	public class NMEAProtocol 
	{
		private const int NP_MAX_CMD_LEN =			8;		// maximum command length (NMEA address)
		private const int NP_MAX_DATA_LEN =			256;	// maximum data length
		private const int NP_MAX_CHAN =			    36;	    // maximum number of channels
		private const int NP_WAYPOINT_ID_LEN =		32;	    // waypoint max string len

		private NMEAState nState;	            // Current state protocol parser is in
		private byte checksum;					// Calculated NMEA sentence checksum
		private byte receivedChecksum;			// Received NMEA sentence checksum (if exists)
		private int index;						// Index used for command and data
		private byte[] command;					// NMEA command
		private byte[] data;					// NMEA data

		// number of NMEA commands received (processed or not processed)
		public int CommandCount;

		public GPGGAData GPGGA;
		public GPGSAData GPGSA = new GPGSAData();
		public GPGSVData GPGSV = new GPGSVData();
		public GPRMBData GPRMB;
		public GPRMCData GPRMC;
		public GPZDAData GPZDA;

		public NMEAProtocol()
		{
			command = new byte[NP_MAX_CMD_LEN];
			data = new byte[NP_MAX_DATA_LEN];
		}

		public void ParseBuffer(byte[] buffer)
		{
			foreach(byte b in buffer)
			{
				ProcessNMEA(b);
			}
			return;
		}

		public void ProcessNMEA(byte btData)
		{
			switch(nState)
			{
					///////////////////////////////////////////////////////////////////////
					// Search for start of message '$'
				case NMEAState.NP_STATE_SOM :
					if(btData == '$')
					{
						checksum = 0;			// reset checksum
						index = 0;				// reset index
						nState = NMEAState.NP_STATE_CMD;
					}
					break;

					///////////////////////////////////////////////////////////////////////
					// Retrieve command (NMEA Address)
				case NMEAState.NP_STATE_CMD :
					if(btData != ',' && btData != '*')
					{
						command[index++] = btData;
						checksum ^= btData;

						// Check for command overflow
						if(index >= NP_MAX_CMD_LEN)
						{
							nState = NMEAState.NP_STATE_SOM;
						}
					}
					else
					{
						command[index] = (byte)'\0';	// terminate command
						checksum ^= btData;
						index = 0;
						nState = NMEAState.NP_STATE_DATA;		// goto get data state
					}
					break;

					///////////////////////////////////////////////////////////////////////
					// Store data and check for end of sentence or checksum flag
				case NMEAState.NP_STATE_DATA:
					if(btData == '*') // checksum flag?
					{
						data[index] = (byte)'\0';
						nState = NMEAState.NP_STATE_CHECKSUM_1;
					}
					else // no checksum flag, store data
					{
						//
						// Check for end of sentence with no checksum
						//
						if(btData == '\r')
						{
							data[index] = (byte) '\0';
							ProcessCommand(EncodeToString( command), data);
							nState = NMEAState.NP_STATE_SOM;
							return;
						}

						//
						// Store data and calculate checksum
						//
						checksum ^= btData;
						data[index] = btData;
						if(++index >= NP_MAX_DATA_LEN) // Check for buffer overflow
						{
							nState = NMEAState.NP_STATE_SOM;
						}
					}
					break;

					///////////////////////////////////////////////////////////////////////
				case NMEAState.NP_STATE_CHECKSUM_1:
					if( (btData - '0') <= 9)
					{
						receivedChecksum = (byte)((btData - '0') << 4);
					}
					else
					{
						receivedChecksum = (byte)((btData - 'A' + 10) << 4);
					}

					nState = NMEAState.NP_STATE_CHECKSUM_2;

					break;

					///////////////////////////////////////////////////////////////////////
				case NMEAState.NP_STATE_CHECKSUM_2:
					if( (btData - '0') <= 9)
					{
						receivedChecksum |= (byte)((btData - (byte)'0'));
					}
					else
					{
						receivedChecksum |= (byte)((btData - (byte)'A' + 10));
					}

					if(checksum == receivedChecksum)
					{
						ProcessCommand(EncodeToString(command), data);
					}

					nState = NMEAState.NP_STATE_SOM;
					break;

					///////////////////////////////////////////////////////////////////////
				default : 
					nState = NMEAState.NP_STATE_SOM;
					break;
			}
		}

		#region INMEAProtocol Members

		public void ProcessGPRMB(GPRMBData gprmc)
		{
			// TODO:  Add NMEAProtocol.ProcessGPRMB implementation
		}

		public void ProcessGPZDA(GPZDAData gpzda)
		{
			// TODO:  Add NMEAProtocol.ProcessGPZDA implementation
		}

		public void ProcessGPRMC(string data)
		{
			string[] fields = Regex.Split(data, ",");


			//Time: Hour, Minute, Second
			//Time is Zulu
			GPRMC.Hour = Convert.ToInt32(fields[0].Substring(0, 2));
			GPRMC.Minute = Convert.ToInt32(fields[0].Substring(2, 2));
			GPRMC.Second = Convert.ToInt32(fields[0].Substring(4, 2));

			GPRMC.Day = Convert.ToInt32(fields[8].Substring(0, 2));
			GPRMC.Month = Convert.ToInt32(fields[8].Substring(2, 2));
			GPRMC.Year = Convert.ToInt32(fields[8].Substring(4, 2));

			GPRMC.DataValid = Convert.ToChar(fields[1]);

			//Latitude
			GPRMC.Latitude = Convert.ToDouble(fields[2]) / 100;
			if (fields[3] == "S")
				GPRMC.LatitudeHemisphere = Cardinal.South;
			else
				GPRMC.LatitudeHemisphere = Cardinal.North;


			//Longitude
			GPRMC.Longitude = Convert.ToDouble(fields[4]) / 100;
			if (fields[5] == "E")
				GPRMC.LatitudeHemisphere = Cardinal.East;
			else
				GPRMC.LatitudeHemisphere = Cardinal.West;

			GPRMC.GroundSpeed = Convert.ToDouble(fields[6]);

			//TODO: MagVar and Course
			GPRMC.Count++;
		}

		public void ProcessGPGSV(string data)
		{

			//parses the GPGSV stream to extract satellite information

			string[] fields = Regex.Split(data,",");
			
			uint totalNumberOfMessages = Convert.ToUInt32(fields[0]);

			//make sure the data is OK. valid range is 1..8 channels
			if ((totalNumberOfMessages > 8) || (totalNumberOfMessages <= 0))
				return;

			GPGSV.TotalNumberOfMessages = totalNumberOfMessages;

			//message number
			int nMessageNumber = Convert.ToInt32(fields[1]);
			
			//make sure it is 0..9...is there an inconsistency? 8/9?
			if ((nMessageNumber > 9) || (nMessageNumber < 0))
				return;

			//sats in view
			GPGSV.SatellitesInView  = Convert.ToInt32(fields[2]);

			//for(int iSat = 0; iSat < GPGSV.SatellitesInView; iSat++)
			for (int iSat = 0; iSat < 4; iSat++)
			{
				Satellite sat = new Satellite();

				sat.Id = Convert.ToInt32(fields[3+iSat*4]);
				sat.Elevation = Convert.ToInt32(fields[4+iSat*4]);
				sat.Azimuth = Convert.ToInt32(fields[5+iSat*4]);
				sat.SignalQuality = Convert.ToInt32(fields[6+iSat*4]);
				sat.Used = IsSatelliteUsed(sat.Id);
				
				GPGSV.Satellites.Add(sat);
			}
			GPGSV.Count ++;
		}

		private bool IsSatelliteUsed(int satelliteId)
		{
			if(null == GPGSA.SatsInSolution) return false;

			foreach(int satId in GPGSA.SatsInSolution)
			{
				if (satId == satelliteId) return true;
			}
			return false;
		}	

		

		public void ProcessGPGSA(string data)
		{
			try
			{
				string[] fields = Regex.Split(data, ",");

				GPGSA.Mode = Convert.ToChar(fields[0]);
				GPGSA.mFixMode = Convert.ToByte(fields[1]);
				GPGSA.SatsInSolution = new int[12];
				for (int i = 2; i <= 13; i++)
				{
					   int satId = -1;
					   if (int.TryParse(fields[i], out satId))
					   {
						   if (satId != -1)
						   {
							   GPGSA.SatsInSolution[i - 2] = satId;
						   }
					   }
				}
				GPGSA.PDOP = Convert.ToDouble(fields[14]);
				GPGSA.HDOP = Convert.ToDouble(fields[15]);
				GPGSA.VDOP = Convert.ToDouble(fields[16]);

				GPGSA.Count++;
			}
			catch (Exception e)
			{
				System.Diagnostics.Trace.WriteLine("Chaos in ProcessGPGSA! " + e.ToString());
			}
		}


		public void ProcessGPGGA(string data)
		{
			try
			{
				string[] fields = Regex.Split(data,",");

				//Time: Hour, Minute, Second
				//Time is Zulu
				GPGGA.Hour = Convert.ToInt32( fields[0].Substring(0,2));
				GPGGA.Minute = Convert.ToInt32(fields[0].Substring(2,2));
				GPGGA.Second = Convert.ToInt32(fields[0].Substring(4,2));

				//Latitude
				GPGGA.Latitude = Convert.ToDouble(fields[1])/100;
				if(fields[2]=="S")
				{
					GPGGA.Latitude = GPGGA.Latitude * (-1);
					GPGGA.LatitudeHemisphere = Cardinal.South;
				}
				else
					GPGGA.LatitudeHemisphere = Cardinal.North;

				//Longitude
				GPGGA.Longitude = Convert.ToDouble(fields[3])/100;
				if (fields[4] == "E")
					GPGGA.LatitudeHemisphere = Cardinal.East;
				else
				{
					GPGGA.Longitude = GPGGA.Longitude * (-1);
					GPGGA.LatitudeHemisphere = Cardinal.West;
				}

				//GPS Signal Quality
				GPGGA.GPSQuality = (GPSQuality)Convert.ToUInt32( fields[5] );

				//Satellites
				GPGGA.NumberOfSatellitesInUse = Convert.ToInt32(fields[6]);

				//HDOP
				GPGGA.HDOP = Convert.ToDouble( fields[7] );

				//Altitude
				GPGGA.Altitude = Convert.ToDouble(fields[8]);

				//increase message count
				GPGGA.Count ++;
			}
			catch(Exception e)
			{
				System.Diagnostics.Trace.WriteLine("Chaos in ProcessGPGGA! " + e.ToString());
			}
		}

		public bool IsSatUsedInSolution(int SatelliteID)
		{
			return IsSatelliteUsed(SatelliteID);
		}

		public void Reset()
		{
			// TODO:  Add NMEAProtocol.Reset implementation
		}

		private string EncodeToString(byte[] buffer)
		{
			string s =  System.Text.ASCIIEncoding.GetEncoding(1252).GetString(buffer);

			// TRIM OFF NULLS!
			//s = s.TrimEnd((char)'\0');
			//return s;
			
			//TRIM at the FIRST NULL
			string[] strings = s.Split('\0');
			return strings[0];
		}

		public void ProcessCommand(string sCmd, byte[] bData)
		{
			string data = EncodeToString(bData);
			
			switch(sCmd)
			{
				case "GPGGA":
					ProcessGPGGA(data);
					break;

				case "GPGSA":
					ProcessGPGSA(data);
					break;

				case "GPGSV":
					ProcessGPGSV(data);
					break;

				case "GPRMC":
					ProcessGPRMC(data);
					break;
				
				case "GPRMB":
					//ProcessGPRMB(pData);
					break;
				
				case "GPZDA":
					//ProcessGPZDA(pData);
					break;

				default:
					break;
			}
			CommandCount = CommandCount + 1;
		}
		#endregion
	}
}