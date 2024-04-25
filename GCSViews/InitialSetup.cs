﻿using log4net;
using MissionPlanner.ArduPilot;
using MissionPlanner.Controls;
using MissionPlanner.Controls.BackstageView;
using MissionPlanner.GCSViews.ConfigurationView;
using MissionPlanner.Radio;
using MissionPlanner.Utilities;
using System;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;

namespace MissionPlanner.GCSViews
{
    public partial class InitialSetup : MyUserControl, IActivate
    {
        internal static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static string lastpagename = "";

        public InitialSetup()
        {
            InitializeComponent();
        }

        public bool isConnected
        {
            get { return MainV2.comPort.BaseStream.IsOpen; }
        }

        public bool isDisConnected
        {
            get { return !MainV2.comPort.BaseStream.IsOpen; }
        }

        public bool isTracker
        {
            get { return isConnected && MainV2.comPort.MAV.cs.firmware == Firmwares.ArduTracker; }
        }

        public bool isCopter
        {
            get { return isConnected && MainV2.comPort.MAV.cs.firmware == Firmwares.ArduCopter2; }
        }

        public bool isCopter35plus
        {
            get { return MainV2.comPort.MAV.cs.version >= Version.Parse("3.5"); }
        }

        public bool isHeli
        {
            get { return isConnected && MainV2.comPort.MAV.aptype == MAVLink.MAV_TYPE.HELICOPTER; }
        }

        public bool isQuadPlane
        {
            get
            {
                return isConnected && isPlane &&
                       MainV2.comPort.MAV.param.ContainsKey("Q_ENABLE") &&
                       (MainV2.comPort.MAV.param["Q_ENABLE"].Value == 1.0);
            }
        }

        public bool isPlane
        {
            get
            {
                return isConnected &&
                       (MainV2.comPort.MAV.cs.firmware == Firmwares.ArduPlane ||
                        MainV2.comPort.MAV.cs.firmware == Firmwares.Ateryx);
            }
        }

        public bool isRover
        {
            get { return isConnected && MainV2.comPort.MAV.cs.firmware == Firmwares.ArduRover; }
        }

        public bool gotAllParams
        {
            get
            {
                log.InfoFormat("TotalReceived {0} TotalReported {1}", MainV2.comPort.MAV.param.TotalReceived,
                    MainV2.comPort.MAV.param.TotalReported);
                if (MainV2.comPort.MAV.param.TotalReceived < MainV2.comPort.MAV.param.TotalReported)
                {
                    return false;
                }

                return true;
            }
        }

        private BackstageViewPage AddBackstageViewPage(Type userControl, string headerText, bool enabled = true,
    BackstageViewPage Parent = null, bool advanced = false)
        {
            try
            {
                if (enabled)
                    return backstageView.AddPage(userControl, headerText, Parent, advanced);
            }
            catch (Exception ex)
            {
                log.Error(ex);
                return null;
            }

            return null;
        }

        public void Activate()
        {
        }

        private void HardwareConfig_Load(object sender, EventArgs e)
        {
            ResourceManager rm = new ResourceManager(this.GetType());

            if (!gotAllParams)
            {
                if (MainV2.comPort.BaseStream.IsOpen)
                    AddBackstageViewPage(typeof(ConfigParamLoading), Strings.Loading);
            }    


            if (MainV2.DisplayConfiguration.displayAccelCalibration)
            {
                AddBackstageViewPage(typeof(ConfigAccelerometerCalibration), rm.GetString("backstageViewPageaccel.Text"), isConnected && gotAllParams);
            }


            if (MainV2.DisplayConfiguration.displayCompassConfiguration)
            {
                if (MainV2.comPort.MAV.param.ContainsKey("COMPASS_PRIO1_ID"))
                    AddBackstageViewPage(typeof(ConfigHWCompass2), rm.GetString("backstageViewPagecompass.Text"),
                        isConnected && gotAllParams);
                else
                    AddBackstageViewPage(typeof(ConfigHWCompass), rm.GetString("backstageViewPagecompass.Text"),
                        isConnected && gotAllParams);
            }
            if (MainV2.DisplayConfiguration.displayRadioCalibration)
            {
                AddBackstageViewPage(typeof(ConfigRadioInput), rm.GetString("backstageViewPageradio.Text"), isConnected && gotAllParams);
            }
            if (MainV2.DisplayConfiguration.displayFlightModes)
            {
                AddBackstageViewPage(typeof(ConfigFlightModes), rm.GetString("backstageViewPageflmode.Text"), isConnected && gotAllParams);
            }

            if (MainV2.DisplayConfiguration.displayMotorTest)
            {
                AddBackstageViewPage(typeof(ConfigMotorTest), rm.GetString("backstageViewPageMotorTest.Text"), isConnected && gotAllParams);
            }       

            // remeber last page accessed
            foreach (BackstageViewPage page in backstageView.Pages)
            {
                if (page.LinkText == lastpagename && page.Show)
                {
                    backstageView.ActivatePage(page);
                    break;
                }
            }

            ThemeManager.ApplyThemeTo(this);
        }

        private void HardwareConfig_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (backstageView.SelectedPage != null)
                lastpagename = backstageView.SelectedPage.LinkText;

            backstageView.Close();
        }
    }
}