using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Model;
using System.Linq;
using System.Data;

namespace GreenQloud.Persistence.SQLite
{
    public class SQLiteRepositoryDAO : RepositoryDAO
    {

        #region implemented abstract members of RepositoryDAO

        SQLiteDatabase database = SQLiteDatabase.Instance();

        public override void Create (LocalRepository e)
        {
            database.ExecuteNonQuery (string.Format("INSERT INTO Repository (Path, RECOVERING, RemoteFolder) VALUES (\"{0}\", \"{1}\", \"{2}\")", e.Path, e.Recovering.ToString(), e.RemoteFolder));
        }

        public void Update (LocalRepository repo)
        {
            database.ExecuteNonQuery (string.Format("UPDATE Repository SET Path=\"{0}\", RECOVERING=\"{1}\", RemoteFolder = \"{2}\" WHERE RepositoryID='{3}'", repo.Path, repo.Recovering.ToString(), repo.RemoteFolder, repo.Id));
        }

        public override List<LocalRepository> All {
            get {        
              return Select("SELECT * FROM REPOSITORY");
            }
        }

        public void DeleteAll ()
        {
            database.ExecuteNonQuery ("DELETE FROM REPOSITORY");
        }
        #endregion

        public LocalRepository GetRepositoryByItemFullName (string itemFullName)
        {
            return All.First (r=> itemFullName.StartsWith(r.Path));
        }

        public LocalRepository FindOrCreate (string root, string remoteFolder)
        {
            List<LocalRepository> repos = Select(string.Format("SELECT * FROM REPOSITORY WHERE PATH == '{0}' AND RemoteFolder == '{1}'", root, remoteFolder));
            LocalRepository repo;
            if (repos.Count > 0) {
                repo = repos.First ();
            } else {
                repo = new LocalRepository (root, remoteFolder);
                repo.Recovering = true;
                Create (repo);
                return FindOrCreate (root, remoteFolder);
            }
            return repos.First();
        }

        public LocalRepository GetById (int id)
        {
            List<LocalRepository> repos = Select(string.Format("SELECT * FROM REPOSITORY WHERE RepositoryID = '{0}'", id));
            return repos.First();
        }


        public List<LocalRepository> Select (string sql){
            List<LocalRepository> repos = new List<LocalRepository>();
            DataTable dt = database.GetDataTable(sql);
            foreach(DataRow dr in dt.Rows){
                LocalRepository r = new LocalRepository (dr [1].ToString (), dr[3].ToString());
                r.Id = int.Parse (dr[0].ToString());
                if(dr[2].ToString().Length > 0)
                    r.Recovering = bool.Parse (dr[2].ToString());
                repos.Add (r);
            }
            return repos;
        }
    }

}

