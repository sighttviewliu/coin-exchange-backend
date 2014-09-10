﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CoinExchange.IdentityAccess.Domain.Model.SecurityKeysAggregate
{
    /// <summary>
    /// Permission
    /// </summary>
    [Serializable]
    [DataContract]
    public class Permission
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public Permission()
        {
            
        }

        /// <summary>
        /// Parameterized Constructor
        /// </summary>
        /// <param name="permissionId"></param>
        /// <param name="permissionName"></param>
        public Permission(string permissionId, string permissionName)
        {
            PermissionId = permissionId;
            PermissionName = permissionName;
        }

        /// <summary>
        /// Permission ID
        /// </summary>
        [DataMember]
        public string PermissionId { get; set; }

        /// <summary>
        /// Permission Name
        /// </summary>
        [DataMember]
        public string PermissionName { get; set; }
    }
}