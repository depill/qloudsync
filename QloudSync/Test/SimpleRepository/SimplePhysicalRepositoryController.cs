using System;
using GreenQloud.Synchrony;
using System.IO;
using GreenQloud.Test.SimpleRepository;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;
using GreenQloud.Model;
using GreenQloud.Repository;
using System.Linq;
using System.Collections.Generic;

namespace GreenQloud.Test.SimpleRepository
{
    public class SimplePhysicalRepositoryController : PhysicalRepositoryController
	{
        public Dictionary<RepositoryItem, string> list = new Dictionary<RepositoryItem, string>();
       
        public SimplePhysicalRepositoryController (SimpleLogicalRepositoryController logical)
        {
            throw new NotImplementedException ();
        }

        public SimplePhysicalRepositoryController (){
        }
       
        #region implemented abstract members of PhysicalRepositoryController


        public override bool IsSync (RepositoryItem item)
        {
            return item.LocalMD5Hash == item.RemoteMD5Hash;
        }

        public override bool Exists (RepositoryItem repoObject)
        {
            bool exists = list.Any(r=> r.Key.FullLocalName == repoObject.FullLocalName);
            return exists;
        }

        public override RepositoryItem CreateItemInstance (string fullLocalName)
        {

           if (list.Any (o=> o.Key.FullLocalName == fullLocalName)){
                return list.First (o=> o.Key.FullLocalName == fullLocalName).Key;
           }
           return null;
        }

        public override List<RepositoryItem> Items {
            get {
                List<RepositoryItem> templist = new List<RepositoryItem>();
                templist.AddRange (list.Keys);
                return templist;
            }
        }

        public override void Delete (RepositoryItem  item)
        {
            if (list.Any (o=> o.Key.FullLocalName ==  item.FullLocalName))
            {
                RepositoryItem temp = list.First (o=> o.Key.FullLocalName ==  item.FullLocalName).Key;
                list.Remove (temp);
            }
        }

        public override RepositoryItem GetCopy (RepositoryItem remoteItem)
        {
            if (list.Any (k=> k.Key.RemoteMD5Hash == remoteItem.RemoteMD5Hash && remoteItem.FullLocalName != k.Key.FullLocalName))
            {
                return list.First (k=> k.Key.RemoteMD5Hash == remoteItem.RemoteMD5Hash && remoteItem.FullLocalName != k.Key.FullLocalName).Key;
            }
            else
                return null;
        }

        public override List<RepositoryItem> GetSubRepositoyItems (RepositoryItem item)
        {
            throw new NotImplementedException ();
        }

        #endregion


        public void Create (RepositoryItem  item)
        {
            Create (item, "");
        }

        public void Create (RepositoryItem  item, string value){
            list.Add ( item, value);
        }

        public string GetValue (string fullLocalName)
        {
            if (list.Any (o=> o.Key.FullLocalName==fullLocalName)){
                string value = list.First (o=> o.Key.FullLocalName==fullLocalName).Value;
                return value;
            }
            return null;
        }
    }

}
