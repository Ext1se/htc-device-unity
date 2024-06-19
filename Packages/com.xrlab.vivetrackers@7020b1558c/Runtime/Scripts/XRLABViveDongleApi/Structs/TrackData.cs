﻿namespace VIVE_Trackers
{
    [System.Serializable]
    public struct TrackData
    {
        public enum Status
        {
            None = 0,
            PoseAndRotation = 2,
            Rotation = 3,
            PoseFrozen = 4,
            PoseAndRotationIdle = 18,
            RotationIdle = 19,
            PoseFrozenIdle = 20,
            PowerOff = -1,
            Standby = -2,
            Reset = -3
        }

        public byte frame_idx;
        public byte btns;
        //public byte RoleID;

        public float pos_x, pos_y, pos_z;
        public float rot_x, rot_y, rot_z, rot_w;
        public float acc_x, acc_y, acc_z;
        public float rot_vel_x, rot_vel_y, rot_vel_z;//, rot_vel_w;
        public float timeMS;
        public float deltaTimeMS;
        //public Dictionary<byte, byte> mapQualityData;
        public byte mapPointQuality;
        public byte mapPointQualityMode;
        /// <summary>
        /// tracking_status = 2 => pose + rot
        /// tracking_status = 3 => rot only
        /// tracking_status = 4 => pose frozen (lost tracking), rots
        /// </summary>
        //public byte tracking_status;

        public Status status ;//=> (Status)tracking_status;

        public override string ToString()
        {
            //var status = ConstantsChorusdStatus.PoseStatusToStr(tracking_status);
            return $"Frame index:{frame_idx}, " +
                   $"Status:{status}, " +
                   $"btns:{btns:X}, " +
                   $"pos: x:{pos_x}, y:{pos_y}, z:{pos_z}, " +
                   $"rot: x:{rot_x}, y:{rot_y}, z:{rot_z}, w:{rot_w}, " +
                   $"acc: x:{acc_x}, y:{acc_y}, z:{acc_z}, " +
                   $"rot_vel: x:{rot_vel_x}, y:{rot_vel_y}, z:{rot_vel_z}, " +
                   $"Time:{timeMS}, " +
                   $"DeltaTime:{deltaTimeMS}, " +
                   $"Map quality:{mapPointQuality}, " +
                   $"MapQualityMode:{mapPointQualityMode}";
        }
    } 
}