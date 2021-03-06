using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading;

using GreenQloud.Model;
using GreenQloud.Repository;
using GreenQloud.Util;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;
using GreenQloud.Persistence.SQLite;
using System.Net.Sockets;
using LitS3;
using GreenQloud.Core;

namespace GreenQloud.Synchrony
{
    
    public class SynchronizerUnit
    {
        private LocalRepository repo;
        private LocalEventsSynchronizer localSynchronizer;
        private RemoteEventsSynchronizer remoteSynchronizer;
        private RecoverySynchronizer recoverySynchronizer;
        private SynchronizerResolver synchronizerResolver;

        private static Dictionary<LocalRepository, SynchronizerUnit> synchronizerUnits = new Dictionary<LocalRepository, SynchronizerUnit>(new GreenQloud.Model.LocalRepository.LocalRepositoryComparer());

		public LocalEventsSynchronizer LocalEventsSynchronizer {
			get {
				return localSynchronizer;
			}
        }

        public static SynchronizerUnit GetByRepo (LocalRepository repo)
        {
            SynchronizerUnit unit;
            if (synchronizerUnits.TryGetValue (repo, out unit)) {
                return unit;
            } else {
                return null;
            }
        }

        public static void Add (LocalRepository repo, SynchronizerUnit unit)
        {
            synchronizerUnits.Add(repo, unit);
        }

        public static bool AnyDownloading ()
        {
            foreach(SynchronizerUnit unit in synchronizerUnits.Values){
                if (unit.IsDownloading) {
                    return true;
                }
            }
            return false;
        }

        public static bool AnyUploading ()
        {
            foreach(SynchronizerUnit unit in synchronizerUnits.Values){
                if (unit.IsUploading) {
                    return true;
                }
            }
            return false;
        }

        public static bool AnyWorking ()
        {
            foreach(SynchronizerUnit unit in synchronizerUnits.Values){
                if (unit.IsWorking) {
                    return true;
                }
            }
            return false;
        }
        
        public static void ReconnectAll ()
        {
            foreach(SynchronizerUnit unit in synchronizerUnits.Values){
                unit.Reconnect ();
            }
        }

        public static void DisconnectAll ()
        {
            foreach(SynchronizerUnit unit in synchronizerUnits.Values){
                unit.Disconnect ();
            }
        }

        public static void ReconnectResolver()
        {
            foreach (SynchronizerUnit unit in synchronizerUnits.Values) {
                unit.ResumeResolver ();
            }
        }

        public static void DisconnectResolver ()
        {
            foreach (SynchronizerUnit unit in synchronizerUnits.Values) {
                unit.SuspendResolver ();
            }
        }
        
        public SynchronizerUnit (LocalRepository repo)
        {
            this.repo = repo;
            synchronizerResolver = SynchronizerResolver.NewInstance (this.repo);
            recoverySynchronizer = RecoverySynchronizer.NewInstance (this.repo);
            remoteSynchronizer = RemoteEventsSynchronizer.NewInstance (this.repo);
            localSynchronizer = LocalEventsSynchronizer.NewInstance (this.repo);
        }

        public void InitializeSynchronizers (bool initRecovery)
        {
            if (initRecovery) {
                recoverySynchronizer.Start ();
                while (!((RecoverySynchronizer)recoverySynchronizer).StartedSync)
                    Thread.Sleep (1000);
                synchronizerResolver.Start (); 

                while (!recoverySynchronizer.FinishedSync) {
                    Thread.Sleep (1000);
                }
            } else {
                synchronizerResolver.Start ();
            }
            localSynchronizer.Start ();
            remoteSynchronizer.Start ();
        }

        public void StopAll ()
        {
            if(synchronizerResolver != null)
                synchronizerResolver.Stop();
            if(recoverySynchronizer != null)
                recoverySynchronizer.Stop();
            if(localSynchronizer != null)
                localSynchronizer.Stop();
            if(remoteSynchronizer != null)
                remoteSynchronizer.Stop();
        }

        public void ResumeResolver()
        {
            if (synchronizerResolver != null) {
                synchronizerResolver.Start ();
            }
        }

        public void Reconnect ()
        {
			if(remoteSynchronizer != null)
				remoteSynchronizer.Start ();
			if(synchronizerResolver != null)
				synchronizerResolver.Start ();
        }

        public void SuspendResolver(){
            if (synchronizerResolver != null) {
                synchronizerResolver.Stop ();
            }
        }

        public void Disconnect ()
        {
			if(remoteSynchronizer != null)
				remoteSynchronizer.Stop ();
			if(synchronizerResolver != null)
				synchronizerResolver.Stop ();
        }

        public bool IsWorking {
            get {
                if (synchronizerResolver.SyncStatus == SyncStatus.DOWNLOADING || synchronizerResolver.SyncStatus == SyncStatus.UPLOADING) {
                    return true;
                } else {
                    return false;
                }
			}
        }
        public bool IsDownloading {
			get {
                if (synchronizerResolver.SyncStatus == SyncStatus.DOWNLOADING) {
					return true;
				} else {
					return false;
				}
			}
        }
        public bool IsUploading {
            get {
                if (synchronizerResolver.SyncStatus == SyncStatus.UPLOADING) {
                    return true;
                } else {
                    return false;
                }
            }
        }
    }
}