﻿using System;
using System.Collections.Generic;
using Microsoft.ApplicationServer.Caching;
using dk.nita.saml20.session;

namespace dk.nita.saml20.ext.appfabricsessioncache
{
    public class AppFabricSessions : AbstractSessions
    {
        private static readonly object Locker = new object();

        // Use configuration from the application configuration file.
        private static readonly DataCacheFactory CacheFactory = new DataCacheFactory();

        public static string CacheName;

        static AppFabricSessions()
        {
            CacheName = System.Configuration.ConfigurationManager.AppSettings["CacheName"];
            if(CacheName == null)
                throw new InvalidOperationException("Not able to initialize AppFabricSessions because no app setting with key CacheName was found.");
        }

        protected override ISession GetSession()
        {
            ISession session = null;
            DataCache sessions = CacheFactory.GetCache(CacheName);
            if (SessionId.HasValue && sessions.Get(SessionId.ToString()) != null)
            {
                sessions.ResetObjectTimeout(SessionId.ToString(), new TimeSpan(0, 0, SessionTimeout, 0));
                    // Needed in order to simluate sliding expiration
                session =  new AppFabricSession(SessionId.Value);
            }

            if (session == null)
            {
                session = new AppFabricSession(CreateSession());
                session.New = true;
            }
            
            return session;
        }

        public override void AbandonSession(Guid sessionId)
        {
            lock (Locker)
            {
                DataCache sessions = CacheFactory.GetCache(CacheName);
                sessions.Remove(sessionId.ToString()); // Remove is not thread safe.
            }
        }

        private Guid CreateSession()
        {
            Guid sessionId = Guid.NewGuid();
            DataCache sessions = CacheFactory.GetCache(CacheName);

            sessions.Add(sessionId.ToString(), new Dictionary<string, object>(), new TimeSpan(0, 0, SessionTimeout, 0));

            return sessionId;
        }
    }
}
