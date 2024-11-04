using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using FZ_Control;

namespace OMRONVision
{
    public class FH_Control
    {
        public CoreRA VisCore = null;

        public string _SimulatorPath { get; set; } = @"C:\OMRON\FZ_FH_FJ_Simulator\651";
        public ConnectionMode _ConnMode { get; set; } = ConnectionMode.Remote;
        public string _IpAddr { get; set; } = "10.5.6.100";
        public int _LineNo { get; set; } = 0;
        public int _DispSize { get; set; } = 320;

        //private readonly object _lockObject = new object();
        public FH_Control()
        {
            //lock (_lockObject)
            //{
                VisCore = new CoreRA();
                //VisCore.MeasureOut += visMeasureOut;
                //VisCore.SceneChange += visSceneChange;
            //}
        }

        ~FH_Control()
        {
            VisCore.Dispose();
        }

        #region Private Methods
        private void visMeasureOut()
        {
            OnMeasureComplete?.Invoke();
        }

        private void visSceneChange(bool initial)
        {
            OnSceneChange?.Invoke();
        }

        private int macroExec(string sCmd)
        {
            if (VisCore.IsConnected)
            {
                return VisCore.Macro_DirectExecute(sCmd);
            }
            return -1;
        }

        private string macroGetVar(string sVar)
        {
            if (VisCore.IsConnected)
            {
                StringBuilder data = new StringBuilder(256);
                VisCore.Macro_GetVariable(sVar, data, 256);
                return data.ToString();
            }
            return "ERR";
        }
        #endregion

        #region Public Methods
        public bool Connect()
        {
            if (!VisCore.IsConnected)
            {
                VisCore.FzPath = _SimulatorPath;
                VisCore.ConnectMode = _ConnMode;
                VisCore.IpAddress = _IpAddr;
                VisCore.LineNo = _LineNo;
                VisCore.DispImageTransferSize = _DispSize;
                VisCore.ConnectStart();
            }
            return VisCore.IsConnected;
        }

        public void Dispose()
        {
            VisCore.Disconnect();
            VisCore.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="wait">
        /// 0: continue subsequent lines without waiting for measurement to end
        /// 1: wait for Measurement to End
        /// 2: wait for Measurement and Result Display to End
        /// </param>
        public void Measure(int wait = 2)
        {
            if (_ConnMode == ConnectionMode.Remote)
            {
                if (wait > 2)
                    wait = 2;
                if (wait < 0)
                    wait = 0;
                macroExec("Measure " + wait);
            }
        }
      
        public void ChangeScene(int sceneNo = 0)
        {
            if (sceneNo >= 0 && sceneNo <= 127)
            {
                macroExec("MeasureStop");
                macroExec("ChangeScene " + sceneNo);
                macroExec("MeasureStart");
            }
        }
      
        public void ClearMeas()
        {
            macroExec("MeasureStop");
            macroExec("ClearMeasureData");
            macroExec("MeasureStart");
        }
      
        public void DataSave()
        {
            macroExec("SaveData");
        }
      
        public string GetSceneTitle()
        {
            return macroGetVar("SceneTitle$(SceneNo)");
        }
      
        public int GetSceneNo()
        {
            return Convert.ToInt32(macroGetVar("SceneNo"));
        }
      
        // SetUnitData
        public int SetUnitData(int unitNo, int dataIdent, string data)
        {
            string sCode = "SetUnitData " + unitNo.ToString() + "," + dataIdent.ToString() + "," + data;
            return macroExec(sCode);
        }
        public int SetUnitData(int unitNo, string dataIdent, string data)
        {
            string sCode = "SetUnitData " + unitNo.ToString() + ",\"" + dataIdent + "\"," + data;
            return macroExec(sCode);
        }
      
        // GetUnitData
        public string GetUnitData(int unitNo, int dataIdent)
        {
            string sCode = "GetUnitData " + unitNo.ToString() + "," + dataIdent.ToString() + ",tempData$";
            if (macroExec(sCode) == 0)
                return macroGetVar("tempData$");
            return "-1";
        }
        public string GetUnitData(int unitNo, string dataIdent)
        {
            string sCode = "GetUnitData " + unitNo.ToString() + ",\"" + dataIdent + "\",tempData$";
            if (macroExec(sCode) == 0)
                return macroGetVar("tempData$");
            return "-1";
        }

        // Get UnitCount
        public int GetUnitCount()
        {
            int count = 0;
            int.TryParse(macroGetVar("UnitCount"), out count);
            return count;
            //return Convert.ToInt32(macroGetVar("UnitCount"));
        }

        // Load Scene From File
        public void LoadScene(int scn = 0, string scnFile = "")
        {
            if (!File.Exists(scnFile))
                return;
            macroExec("LoadScene " + scn + ", \"" + scnFile + "\"");
        }

        #endregion
        public delegate void MeasureComplete();
        public delegate void SceneChange();

        public event MeasureComplete OnMeasureComplete = delegate { };
        public event SceneChange OnSceneChange = delegate { };

        // SetSystemData
        public int SetSystemData(string dataIdent0, string dataIdent1, string data)
        {
            macroExec("MeasureStop");
            string sCode = "SetSystemData \"" + dataIdent0 + "\",\"" + dataIdent1 + "\",\"" + data + "\"";
            int ret = macroExec(sCode);
            macroExec("MeasureStop");
            return ret;
        }

        // re-establish network drive connection
        public void Reconnect()
        {
            SetSystemData("NetworkDrive", "reconnect", "1");
        }

        // programatically save last logging image
        public void SaveImage(string filename = "")
        {
            string name = @"S:OMRON\_RAMDisk\_LastLoggingImage\" + filename + ".ifz";
            macroExec("SaveImage -1, \"" + name + "\"");
        }
    }
}
