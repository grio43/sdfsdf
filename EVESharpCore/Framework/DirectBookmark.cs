// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias SC;

using EVESharpCore.Cache;
using SC::SharedComponents.Py;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Logging;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public enum BookmarkType
    {
        Station,
        Citadel,
        Solar_System,
        Coordinate,
    }

    public class DirectBookmark : DirectInvType
    {
        #region Fields

        /// <summary>
        ///     Entity cache
        /// </summary>
        private DirectEntity _entity;

        public const int BOOKMARK_EXPIRY_NONE = 0;
        public const int BOOKMARK_EXPIRY_3HOURS = 1;
        public const int BOOKMARK_EXPIRY_2DAYS = 2;

        #endregion Fields

        #region Constructors

        internal DirectBookmark(DirectEve directEve, PyObject pyBookmark)
            : base(directEve)
        {
            PyBookmark = pyBookmark;
            BookmarkId = (long?)pyBookmark.Attribute("bookmarkID");
            CreatedOn = (DateTime?)pyBookmark.Attribute("created");
            ItemId = (long?)pyBookmark.Attribute("itemID");
            LocationId = (long?)pyBookmark.Attribute("locationID");
            FolderId = (long?)pyBookmark.Attribute("folderID");
            Title = (string)pyBookmark.Attribute("memo");
            if (!String.IsNullOrEmpty(Title) && Title.Contains("\t"))
            {
                Memo = Title.Substring(Title.IndexOf("\t") + 1);
                Title = Title.Substring(0, Title.IndexOf("\t"));
            }

            Note = (string)pyBookmark.Attribute("note");
            OwnerId = (int?)pyBookmark.Attribute("ownerID");
            TypeId = (int)pyBookmark.Attribute("typeID");
            X = (double?)pyBookmark.Attribute("x");
            Y = (double?)pyBookmark.Attribute("y");
            Z = (double?)pyBookmark.Attribute("z");

            if (Enum.TryParse<BookmarkType>(GroupName.Replace(" ", "_"), out var result))
            {
                BookmarkType = result;
                if (BookmarkType != BookmarkType.Citadel && BookmarkType != BookmarkType.Station && X.HasValue && Y.HasValue && Z.HasValue)
                    BookmarkType = BookmarkType.Coordinate;
            }
        }

        #endregion Constructors

        #region Properties

        public long? BookmarkId { get; internal set; }
        public BookmarkType BookmarkType { get; private set; }
        public DateTime? CreatedOn { get; internal set; }


        public bool DockedAtBookmark()
        {
            if ((BookmarkType == BookmarkType.Station || BookmarkType == BookmarkType.Citadel)
                && ItemId.HasValue
                && (ItemId == ESCache.Instance.DirectEve.Session.StationId
                    || ItemId == ESCache.Instance.DirectEve.Session.Structureid))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        ///     The entity associated with this bookmark
        /// </summary>
        /// <remarks>
        ///     This property will be null if no entity can be found
        /// </remarks>
        public DirectEntity Entity => _entity ?? (_entity = DirectEve.GetEntityById(ItemId ?? -1));

        public long? FolderId { get; internal set; }
        public bool IsInCurrentSystem => LocationId == DirectEve.Session.LocationId || ItemId == DirectEve.Session.LocationId || LocationId == DirectEve.Session.SolarSystemId || ItemId == DirectEve.Session.SolarSystemId;

        /// <summary>
        ///     If this is a bookmark of a station, this is the StationId
        /// </summary>
        public long? ItemId { get; internal set; }

        /// <summary>
        ///     Matches SolarSystemId
        /// </summary>
        public long? LocationId { get; internal set; }

        public string Memo { get; internal set; }
        public string Note { get; internal set; }
        public int? OwnerId { get; internal set; }
        public string Title { get; internal set; }
        public double? X { get; internal set; }
        public double? Y { get; internal set; }
        public double? Z { get; internal set; }
        public Vec3 Pos => new Vec3(X ?? 0, Y ?? 0, Z ?? 0);
        public DirectWorldPosition WorldPosition => new DirectWorldPosition(X ?? 0, Y ?? 0, Z ?? 0);
        internal PyObject PyBookmark { get; set; }

        #endregion Properties

        #region Methods

        public bool Approach()
        {

            if (!IsInCurrentSystem)
            {
                DirectEve.Log("ERROR: The bookmark is not in the current system!");
                return false;
            }
                

            if (!DirectEve.ActiveShip.CanWeMove)
                return false;

            var approachLocation = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.movementFunctions").Attribute("ApproachLocation");
            return DirectEve.ThreadedCall(approachLocation, PyBookmark);
        }

        //public bool CopyBookmarksToCorpFolder()
        //{
        //    if (!BookmarkId.HasValue || DirectEve.Session.CorporationId == null)
        //        return false;

        //    return DirectEve.ThreadedLocalSvcCall("bookmarkSvc", "MoveBookmarksToFolder", DirectEve.Session.CorporationId, DirectEve.Session.CorporationId,
        //        PyBookmark.Attribute("bookmarkID"));
        //}

        public double DistanceTo(DirectStation station)
        {
            if (this.BookmarkType != BookmarkType.Coordinate)
                return double.MaxValue;

            return Math.Round(Math.Sqrt((station.X - X.Value) * (station.X - X.Value) + (station.Y - Y.Value) * (station.Y - Y.Value) + (station.Z - Z.Value) * (station.Z - Z.Value)), 2);
        }

        public double DistanceTo(DirectEntity entity)
        {
            if (this.BookmarkType != BookmarkType.Coordinate)
                return double.MaxValue;

            return Math.Round(Math.Sqrt((entity.X - X.Value) * (entity.X - X.Value) + (entity.Y - Y.Value) * (entity.Y - Y.Value) + (entity.Z - Z.Value) * (entity.Z - Z.Value)), 2);
        }
        
        public double DistanceTo(Vec3 vec3)
        {
            if (this.BookmarkType != BookmarkType.Coordinate)
                return double.MaxValue;

            return Math.Round(Math.Sqrt((vec3.X - X.Value) * (vec3.X - X.Value) + (vec3.Y - Y.Value) * (vec3.Y - Y.Value) + (vec3.Z - Z.Value) * (vec3.Z - Z.Value)), 2);
        }

        public double DistanceTo(DirectBookmark entity)
        {
            if (this.BookmarkType != BookmarkType.Coordinate)
                return double.MaxValue;
            
            if (entity.BookmarkType != BookmarkType.Coordinate)
                return double.MaxValue;

            return Math.Round(Math.Sqrt((double)((entity.X - X.Value) * (entity.X - X.Value) + (entity.Y - Y.Value) * (entity.Y - Y.Value) + (entity.Z - Z.Value) * (entity.Z - Z.Value))), 2);
        }


        public bool Delete()
        {
            if (!BookmarkId.HasValue)
                return false;

            return DirectEve.ThreadedLocalSvcCall("addressbook", "DeleteBookmarks", new List<PyObject> { PyBookmark.Attribute("bookmarkID") });
        }

        //public bool UpdateBookmark(string name, string comment)
        //{
        //    if (!BookmarkId.HasValue)
        //        return false;

        //    return DirectEve.ThreadedLocalSvcCall("bookmarkSvc", "UpdateBookmark", PyBookmark.Attribute("bookmarkID"), PyBookmark.Attribute("ownerID"), name,
        //        comment, PyBookmark.Attribute("folderID"));
        //}

        public bool WarpTo()
        {
            return WarpTo(0);
        }

        public bool WarpTo(double distance)
        {

            if (!DirectEve.ActiveShip.CanWeMove)
                return false;

            if (!IsInCurrentSystem)
            {
                DirectEve.Log("ERROR: The bookmark is not in the current system!");
                return false;
            }

            if (DirectEve.Interval(4000, 5000) && DirectEve.DWM.ActivateWindow(typeof(DirectDesktopWindow), true))
            {
                var warpToBookmark = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.movementFunctions").Attribute("WarpToBookmark");
                return DirectEve.ThreadedCall(warpToBookmark, PyBookmark, distance);
            }
            return false;
        }
        //def ACLBookmarkLocation(self, itemID, folderID, name, comment, itemTypeID, expiry, subfolderID = None):
        internal static bool BookmarkLocation(DirectEve directEve, long itemId, long folderId, string name, int typeId, int expiry = 0, string comment = "")
        {

            if (expiry < 0 || expiry > 2)
                return false;

            var folders = GetFolders(directEve);

            if (!folders.Where(f => f.IsActive).Any(f => f.Id.Equals(folderId)))
                return false;

            var bookmarkLocation = directEve.GetLocalSvc("bookmarkSvc").Attribute("ACLBookmarkLocation");
            return directEve.ThreadedCall(bookmarkLocation, itemId, folderId, name, comment, typeId, expiry);
        }


        //CreateBookmarkFolder(self, isPersonal, folderName, description, adminGroupID, manageGroupID, useGroupID, viewGroupID)
        // self.CreateBookmarkFolder(True, name, '', None, None, None, None)
        internal static bool CreatePersonalBookmarkFolder(DirectEve directEve, string name, string description = "")
        {
            return directEve.ThreadedLocalSvcCall("bookmarkSvc", "CreateBookmarkFolder", true, name, description, PySharp.PyNone, PySharp.PyNone, PySharp.PyNone, PySharp.PyNone);
        }

        internal static List<DirectBookmark> GetBookmarks(DirectEve directEve)
        {
            // List the bookmarks from cache
            var bookmarks = directEve.GetLocalSvc("bookmarkSvc").Attribute("bookmarkCache").ToDictionary<long>();
            return bookmarks.Values.Select(pyBookmark => new DirectBookmark(directEve, pyBookmark)).ToList();
        }

        internal static List<DirectBookmarkFolder> GetFolders(DirectEve directEve)
        {
            // List the bookmark folders from cache
            var folders = directEve.GetLocalSvc("bookmarkSvc").Attribute("foldersNew").ToDictionary<long>();
            return folders.Values.Select(pyFolder => new DirectBookmarkFolder(directEve, pyFolder)).ToList();
        }

        internal static DateTime? GetLastBookmarksUpdate(DirectEve directEve)
        {
            // Get the bookmark-last-update-time
            return (DateTime?)directEve.GetLocalSvc("bookmarkSvc").Attribute("lastUpdateTime");
        }

        internal static bool RefreshBookmarks(DirectEve directEve)
        {
            // If the bookmarks need to be refreshed, then this will do it
            return directEve.ThreadedLocalSvcCall("bookmarkSvc", "GetBookmarks");
        }

        //internal static bool RefreshPnPWindow(DirectEve directEve)
        //{
        //    return directEve.ThreadedLocalSvcCall("bookmarkSvc", "RefreshWindow");
        //    ;
        //}

        #endregion Methods
    }
}