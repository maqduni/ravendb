﻿namespace Raven.Client.Replication.Messages
{
    public class TopologyDiscoveryResponse
    {
        public Status DiscoveryStatus;

        public string Exception;

        public enum Status
        {
            AlreadyKnown = 1,
            Leaf = 2,
            Ok = 3,
            Error
        }
    }
}
