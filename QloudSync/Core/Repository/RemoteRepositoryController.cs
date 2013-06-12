using System;
using System.Collections.Generic;
using System.Linq;
using GreenQloud.Util;
using GreenQloud.Model;
using GreenQloud.Repository.Local;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.IO;
using System.Text;
using QloudSync.Repository;
using System.Threading;
using LitS3;

namespace GreenQloud.Repository
{
    public class RemoteRepositoryController : AbstractController, IRemoteRepositoryController
    {
        private StorageQloudPhysicalRepositoryController physicalController;
        private S3Connection connection;
        public RemoteRepositoryController (){
            connection = new S3Connection ();
            physicalController = new StorageQloudPhysicalRepositoryController ();
        }

        public List<GreenQloud.Model.RepositoryItem> Items {
            get {
                return GetInstancesOfItems (GetS3Objects().Where(i => !i.Name.StartsWith(".")).ToList());
            }
        }

        public List<GreenQloud.Model.RepositoryItem> AllItems {
            get {
                return GetInstancesOfItems (GetS3Objects());
            }
        }

        public List<GreenQloud.Model.RepositoryItem> TrashItems {
            get {
                return GetInstancesOfItems (GetS3Objects().Where(i => i.Name.StartsWith(Constant.TRASH)).ToList());
            }
        }

        public List<GreenQloud.Model.RepositoryItem> GetCopys (RepositoryItem item)
        {
            return AllItems.Where (rf => rf.ETag == item.LocalETag && rf.Key != item.Key).ToList<RepositoryItem> ();
        }

        public bool ExistsCopies (RepositoryItem item)
        {
            return GetCopys(item).Count > 0;
        }

        //TODO REFACTOR (make a query) 
        public bool Exists (RepositoryItem item)
        {
            return Items.Any (rf => rf.Key == item.Key);
        }

        #region Manage Itens
        public void Download (RepositoryItem item)
        {
            if (item.IsFolder) {
                physicalController.CreateFolder(item);
                DownloadEntry(connection.Connect ().ListObjects (RuntimeSettings.DefaultBucketName, item.Key), item);
            } else {
                GenericDownload (item.Key, item.LocalAbsolutePath);
            }
        }

        private void DownloadEntry(IEnumerable<ListEntry> entries, RepositoryItem father){
            foreach(ListEntry entry in entries ){
                if(Key(entry) != string.Empty){
                    RepositoryItem item = CreateObjectInstance (entry);
                    if(item.Key != father.Key){
                        Download (item);
                    }
                }
            }
        }

        public void Upload (RepositoryItem item)
        {
            if(item.IsFolder){
                GenericUploadFolder (item.Key);
                UploadEntry(connection.Connect ().ListObjects (RuntimeSettings.DefaultBucketName, item.Key), item);
            }else{
                GenericUpload (item.Key,  item.LocalAbsolutePath);
            }
        }

        private void UploadEntry(IEnumerable<ListEntry> entries, RepositoryItem father){
            foreach(ListEntry entry in entries ){
                if(Key(entry) != string.Empty){
                    RepositoryItem item = CreateObjectInstance (entry);
                    if(item.Key != father.Key){
                        Upload (item);
                    }
                }
            }
        }

        public void Move (RepositoryItem item)
        {
            if(item.IsFolder){
                GenericCopy (item.Key, item.ResultItem.Key);
                MoveEntry(connection.Connect ().ListObjects (RuntimeSettings.DefaultBucketName, item.Key), item);
            }else{
                GenericCopy (item.Key, item.ResultItem.Key);
            }

            Delete (item);
        }

        private void MoveEntry(IEnumerable<ListEntry> entries, RepositoryItem father){
            foreach(ListEntry entry in entries ){
                if(Key(entry) != string.Empty){
                    RepositoryItem item = CreateObjectInstance (entry);
                    item.BuildResultItem (Path.Combine(father.ResultItem.Key, item.Name));
                    if(item.Key != father.Key){
                        Move (item);
                    }
                }
            }
        }

        public void Copy (RepositoryItem item)
        {
            if(item.IsFolder){
                GenericCopy (item.Key, item.ResultItem.Key);
                CopyEntry(connection.Connect ().ListObjects (RuntimeSettings.DefaultBucketName, item.Key), item);
            }else{
                GenericCopy (item.Key, item.ResultItem.Key);
            }
        }

        private void CopyEntry(IEnumerable<ListEntry> entries, RepositoryItem father){
            foreach(ListEntry entry in entries ){
                if(Key(entry) != string.Empty){
                    RepositoryItem item = CreateObjectInstance (entry);
                    if(item.Key != father.Key){
                        Copy (item);
                    }
                }
            }
        }

        public void Delete (RepositoryItem item)
        {
            GenericDelete (item.Key, item.IsFolder);
        }

        public string RemoteETAG (RepositoryItem item)
        {
            return GetMetadata(item.Key).ETag;
        }

        public GetObjectResponse GetMetadata (string key)
        {
            S3Service service = connection.Connect ();

            var request = new LitS3.GetObjectRequest(service, RuntimeSettings.DefaultBucketName, key, true);
            using (GetObjectResponse response = request.GetResponse())
                return response;
        }
        #endregion
     

        #region Generic
        private void GenericCopy (string sourceKey, string destinationKey)
        {
            connection.Connect ().CopyObject (RuntimeSettings.DefaultBucketName, sourceKey, destinationKey);
        }

        private void GenericDelete (string key, bool keyAsPrefix = false)
        {
            if (keyAsPrefix) {
                connection.Connect ().ForEachObject (RuntimeSettings.DefaultBucketName, key, DeleteEntry);
                GenericDelete (key);
            } else {
                connection.Connect ().DeleteObject (RuntimeSettings.DefaultBucketName, key);
            }
        }
        private void DeleteEntry(ListEntry entry){
            if(Key(entry) != string.Empty){
                connection.Connect ().DeleteObject (RuntimeSettings.DefaultBucketName, Key(entry));
            }
        }


        private void GenericDownload (string key, string localAbsolutePath)
        {
            BlockWatcher (localAbsolutePath);
            connection.Connect().GetObject(RuntimeSettings.DefaultBucketName, key, localAbsolutePath);
            UnblockWatcher (localAbsolutePath);
        }

        private void GenericUpload (string key, string filepath)
        {
            connection.Connect().AddObject(filepath, RuntimeSettings.DefaultBucketName, key);
        }

        private void GenericUploadFolder (string key)
        {
            string objectContents = string.Empty;
            connection.Connect ().AddObject (RuntimeSettings.DefaultBucketName, key, 0, stream =>
                                                {
                                                    var writer = new StreamWriter(stream, Encoding.ASCII);
                                                    writer.Write(objectContents);
                                                    writer.Flush();
                                                });
        }

        private void DeleteFolder (string key){
            GenericDelete (key, true);
        }

        #endregion


        #region Handle S3Objects
        public IEnumerable<ListEntry> GetS3Objects ()
        {
            return connection.Connect ().ListAllObjects (RuntimeSettings.DefaultBucketName);
        }

        protected List<RepositoryItem> GetInstancesOfItems (IEnumerable<ListEntry> s3items)
        {
            List <RepositoryItem> remoteItems = new List <RepositoryItem> ();
            foreach (ListEntry s3item in s3items) {
                if (!s3item.Name.Contains(Constant.TRASH))
                {    
                    remoteItems.Add ( CreateObjectInstance (s3item));
                    //add folders that have items to persist too                   
                }
            }
            return remoteItems;
        }

        public RepositoryItem CreateObjectInstance (ListEntry s3item)
        {
            string key = Key (s3item);
            if (key != string.Empty) {
                LocalRepository repo;
                repo = new Persistence.SQLite.SQLiteRepositoryDAO ().FindOrCreateByRootName (RuntimeSettings.HomePath);
                RepositoryItem item = RepositoryItem.CreateInstance (repo, GetMetadata (key).ContentLength == 0, s3item);
                return item;
            }
            return null;
        }

        private string Key(ListEntry s3item){
            string key = string.Empty;
            if (s3item is CommonPrefix) {
                key = ((CommonPrefix)s3item).Prefix;
            } else {
                key = ((ObjectEntry)s3item).Key;
            }
            return key;
        }
        #endregion

    }
}

