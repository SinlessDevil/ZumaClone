using System;

namespace Code.Services.PersistenceProgress.Analytic
{
    [Serializable]
    public class User
    {
        public string Id;
        public string Location;
        public string AdvertisingId;

        public User(string id)
        {
            Id = id;
        }
    }
}