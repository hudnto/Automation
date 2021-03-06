﻿using Logging;
using Module;
using System;
using System.Deployment.Application;
using System.Threading.Tasks;

namespace Automation
{
    class SystemControlService : ServiceBase
    {
        public class Status
        {
            public string CurrentVersion { get; set; }
            public string AvailableVersion { get; set; }
            public bool UpdateAvailable { get; set; }
        }

        private Status mStatus;

        private bool mUpdating = false;

        public SystemControlService(ServiceCreationInfo info)
        : base("system", info)
        {
            RefreshStatus();

            Log.Info("Application is running version: " + mStatus.CurrentVersion);
            if (mStatus.UpdateAvailable)
                Log.Info("There is an update available");
        }

        private void RefreshStatus()
        {
            mStatus = new Status();

            bool networkDeployed = ApplicationDeployment.IsNetworkDeployed;
            if (networkDeployed)
            {
                try
                {
                    ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;

                    mStatus.CurrentVersion = ad.CurrentVersion.ToString();
                    mStatus.AvailableVersion = mStatus.CurrentVersion;

                    UpdateCheckInfo updateInfo = ad.CheckForDetailedUpdate();
                    mStatus.AvailableVersion = updateInfo.AvailableVersion.ToString();

                    mStatus.UpdateAvailable = updateInfo.UpdateAvailable;
                }
                catch (InvalidOperationException)
                { }
            }
        }

        [ServiceGetContractAttribute("status")]
        public Status OnGetStatus()
        {
            RefreshStatus();

            return mStatus;
        }

        [ServicePutContract("update")]
        public bool OnUpdateRequest()
        {
            Log.Debug("ControlService: " + this);
            RefreshStatus();

            if (mUpdating)
            {
                Log.Info("Discarding surplus udpate request");
                return false;
            }

            if (!mStatus.UpdateAvailable)
            {
                Log.Info("Attempting to update but there are no new versions available");
                return false;
            }

            bool networkDeployed = ApplicationDeployment.IsNetworkDeployed;
            if (!networkDeployed)
            {
                Log.Info("The application is not network deployed");
                return false;
            }

            mUpdating = true;

            Log.Info("Update requested");

            var deployment = ApplicationDeployment.CurrentDeployment;

            Log.Info("==============================================================");
            Log.Info("==========================UPDATING============================");
            Log.Info("==============================================================");
            deployment.Update();

            Log.Info("==============================================================");
            Log.Info("=========================RESTARTING===========================");
            Log.Info("==============================================================");

            // Run it async so we have a chance to complete the REST call
            Task.Run(async () =>
            {
                await Task.Delay(100);
                Environment.Exit(0);
            });

            return true;
        }

        [ServicePutContract("restart")]
        public void OnRestartRequest()
        {
            Log.Info("Restart requested");

            Log.Info("==============================================================");
            Log.Info("=========================RESTARTING===========================");
            Log.Info("==============================================================");

            // Run it async so we have a chance to complete the REST call
            Task.Run(async () =>
            {
                await Task.Delay(100);
                Environment.Exit(0);
            });
        }
    }
}
