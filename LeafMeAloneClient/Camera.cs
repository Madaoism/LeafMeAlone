﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;

namespace Client
{
    /// <summary>
    /// A class that stores the information necessary to construct the View matrix
    /// CameraPosition: Location of the Camera
    /// CameraLookAt: Location of the target that the camera is looking at
    /// CameraUp: Orientation of the camera; typically (0,0,1)
    /// </summary>
    class Camera
    {
        private Vector3 m_CameraUp;
        public Vector3 CameraUp {
            get
            {
                return m_CameraUp; 
            }
            set
            {
                m_CameraUp = value;
                UpdateCameraView();

            }
        }
        public Vector3 m_CameraLookAt;
        public Vector3 CameraLookAt
        {
            get
            {
                return m_CameraLookAt;
            }
            set
            {
                m_CameraLookAt = value;
                UpdateCameraView();

            }
        }
        public Vector3 m_CameraPosition;
        public Vector3 CameraPosition
        {
            get
            {
                return m_CameraPosition;
            }
            set
            {
                m_CameraPosition = value;
                UpdateCameraView();

            }
        }

        public Matrix m_ViewMatrix;

        /// <summary>
        /// Initialize the Camera parameters and the camera matrix
        /// </summary>
        /// <param name="pos"> position of the camera </param>
        /// <param name="lookat"> target that the camera is looking at </param>
        /// <param name="up"> orientation of the camera </param>
        public Camera( Vector3 pos, Vector3 lookat, Vector3 up)
        {
            m_CameraUp = up;
            m_CameraPosition = pos;
            m_CameraLookAt = lookat;
            UpdateCameraView();
        }

        /// <summary>
        /// Update the Camera matrix
        /// </summary>
        public void UpdateCameraView()
        {
            m_ViewMatrix = Matrix.LookAtLH(m_CameraPosition, m_CameraLookAt, m_CameraUp);
        }

        /// <summary>
        /// move the camera by delta (by default)
        /// can also specify the camera absolute position
        /// </summary>
        /// <param name="delta"> </param>
        /// <param name="relative"> </param>
        public void MoveCamera( Vector3 delta, bool relative=true )
        {
            if (relative)
            {
                m_CameraPosition += delta;
                m_CameraLookAt += delta;
            }
            else
            {
                Vector3 diff = delta - m_CameraPosition;
                m_CameraPosition = delta;
                m_CameraLookAt = m_CameraLookAt + diff;
            }
            UpdateCameraView();
        }


        public void rotateCamera(Vector3 centerOfRotation, Vector3 direction, float angle)
        {

            Vector3 lookatDirection = Vector3.Normalize(centerOfRotation - m_CameraPosition);
            Vector3 rotationAxis = Vector3.Cross(lookatDirection, Vector3.Normalize(direction));

            Matrix rotationMatrix = Matrix.RotationAxis(rotationAxis, angle);

            m_CameraPosition = Vector3.TransformCoordinate(m_CameraPosition, rotationMatrix);
            UpdateCameraView();
        }
    }
}
