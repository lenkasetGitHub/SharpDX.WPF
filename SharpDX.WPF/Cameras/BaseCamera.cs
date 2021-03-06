﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

// made after DXUTCamera.h & DXUTCamera.cpp

namespace SharpDX.WPF.Cameras
{
    /// <summary>
    /// Simple base camera class that moves and rotates.  The base class
    /// records mouse and keyboard input for use by a derived class, and
    /// keeps common state.
    /// </summary>
    public abstract class BaseCamera
    {
        protected Dictionary<Key, bool> m_downKeys = new Dictionary<Key, bool>();

        protected Vector2 m_mouseDownPos, m_mouseLastPos;

        protected Quaternion m_viewRotQuat;

        private static readonly Vector3 s_zero3 = new Vector3();

        private float m_aspect;

        private bool m_enableYAxisMovement = true;

        private float m_farPlane;

        private float m_fov;

        private Vector3 m_lookAt, m_defaultLookAt;

        // Field of view
        // Aspect ratio
        private float m_nearPlane;

        private Vector3 m_position, m_defaultPosition;

        private Matrix m_projMat;

        private Vector3 m_up, m_defaultUp;

        // Projection matrix
        // Near plane
        // Far plane
        private Matrix m_viewMat;

        /// <summary>
        ///
        /// </summary>
        public BaseCamera()
        {
            SetViewParams(new Vector3(), new Vector3(0, 0, 1), new Vector3(0, 1, 0));
            SetProjParams((float)Math.PI / 3, 1, 0.05f, 100.0f);
            OnInitInteractive();
        }

        /// <summary>
        ///
        /// </summary>
        public float AspectRatio
        {
            get { return m_aspect; }
            set
            {
                m_aspect = value;
                UpdateProj();
            }
        }

        /// <summary>
        ///
        /// </summary>
        public bool EnableYAxisMovement
        {
            get { return m_enableYAxisMovement; }
            set
            {
                if (value == m_enableYAxisMovement)
                    return;
                m_enableYAxisMovement = value;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public float FarPlane
        {
            get { return m_farPlane; }
            set
            {
                m_farPlane = value;
                UpdateProj();
            }
        }

        /// <summary>
        ///
        /// </summary>
        public float FieldOfView
        {
            get { return m_aspect; }
            set
            {
                m_fov = value;
                UpdateProj();
            }
        }

        /// <summary>
        ///
        /// </summary>
        public Vector3 LookAt
        {
            get { return m_lookAt; }
            set
            {
                m_lookAt = value;
                UpdateView();
            }
        }

        /// <summary>
        ///
        /// </summary>
        public float MoveScaler { get; set; }

        /// <summary>
        ///
        /// </summary>
        public float NearPlane
        {
            get { return m_nearPlane; }
            set
            {
                m_nearPlane = value;
                UpdateProj();
            }
        }

        /// <summary>
        ///
        /// </summary>
        public Vector3 Position
        {
            get { return m_position; }
            set
            {
                m_position = value;
                UpdateView();
            }
        }

        /// <summary>
        ///
        /// </summary>
        public Matrix Projection { get { return m_projMat; } }

        /// <summary>
        ///
        /// </summary>
        public float RotationScaler { get; set; }

        /// <summary>
        ///
        /// </summary>
        public Vector3 Up
        {
            get { return m_up; }
            set
            {
                m_up = value;
                UpdateView();
            }
        }

        /// <summary>
        ///
        /// </summary>
        public Matrix View { get { return m_viewMat; } }

        /// <summary>
        ///
        /// </summary>
        /// <param name="elapsed"></param>
        public void FrameMove(TimeSpan elapsed)
        {
            float rSpeed = 0;
            Vector3 speed = new Vector3();
            foreach (var item in m_downKeys.Keys)
            {
                speed += GetSpeed(item);
                rSpeed += GetRollSpeed(item);
            }

            KeyMove(speed * (float)elapsed.TotalSeconds);
            //KeyRoll(rSpeed * (float)elapsed.TotalSeconds);
            KeyRotateHorizontal(rSpeed * (float)elapsed.TotalSeconds);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ui"></param>
        /// <param name="e"></param>
        public void HandleKeyDown(UIElement ui, KeyEventArgs e)
        {
            m_downKeys[e.Key] = true;

            switch (e.Key)
            {
                case Key.W:
                case Key.Up:
                case Key.S:
                case Key.Down:
                case Key.D:
                case Key.Right:
                case Key.A:
                case Key.Left:
                case Key.PageUp:
                case Key.PageDown:
                    // speed
                    break;

                case Key.E:
                case Key.Q:
                    // roll speed
                    break;

                case Key.Home:
                    Reset();
                    break;

                default:
                    return;
            }
            e.Handled = true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ui"></param>
        /// <param name="e"></param>
        public void HandleKeyUp(UIElement ui, KeyEventArgs e)
        {
            m_downKeys.Remove(e.Key);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ui"></param>
        /// <param name="e"></param>
        public void HandleMouseDown(UIElement ui, MouseButtonEventArgs e)
        {
            m_mouseDownPos = GetVector(ui, e);
            m_mouseLastPos = m_mouseDownPos;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ui"></param>
        /// <param name="e"></param>
        public void HandleMouseMove(UIElement ui, MouseEventArgs e)
        {
            var pMouse = GetVector(ui, e);
            var dp = pMouse - m_mouseLastPos;

            {
                var rAxis = Vector3.Cross(new Vector3(dp.X, dp.Y, 0), new Vector3(0, 0, -1));
                if (rAxis.LengthSquared() >= 0.00001)
                {
                    float angle = GetMouseAngle(dp, ui);
                    var tmpQuat = Quaternion.RotationAxis(rAxis, angle);
                    MouseRotation(tmpQuat);
                }
            }

            m_mouseLastPos = pMouse;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ui"></param>
        /// <param name="e"></param>
        public void HandleMouseUp(UIElement ui, MouseButtonEventArgs e)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ui"></param>
        /// <param name="e"></param>
        public virtual void HandleMouseWheel(UIElement ui, MouseWheelEventArgs e)
        {
            var dp = e.Delta > 0 ? new Vector3(0, 0, -1) : new Vector3(0, 0, 1);
            KeyMove(dp);
        }

        /// <summary>
        ///
        /// </summary>
        public void Reset()
        {
            SetViewParams(m_defaultPosition, m_defaultLookAt, m_defaultUp);
        }

        /// <summary>
        /// TODO: accept a real angle as a value
        /// </summary>
        /// <param name="angle"></param>
        public void Roll(float angle)
        {
            angle *= RotationScaler;
            var m = Matrix.RotationZ(angle);
            Up = m.TransformNormal(Up);
            UpdateView();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="fFOV"></param>
        /// <param name="fAspect"></param>
        /// <param name="fNearPlane"></param>
        /// <param name="fFarPlane"></param>
        public void SetProjParams(float fFOV, float fAspect, float fNearPlane, float fFarPlane)
        {
            m_fov = fFOV;
            m_aspect = fAspect;
            m_nearPlane = fNearPlane;
            m_farPlane = fFarPlane;
            UpdateProj();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sRotation"></param>
        /// <param name="sMove"></param>
        public void SetScalers(float sRotation, float sMove)
        {
            RotationScaler = sRotation;
            MoveScaler = sMove;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="eye"></param>
        /// <param name="lookAt"></param>
        public void SetViewParams(Vector3 eye, Vector3 lookAt)
        {
            SetViewParams(eye, lookAt, m_up);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="eye"></param>
        /// <param name="lookAt"></param>
        /// <param name="vUp"></param>
        public virtual void SetViewParams(Vector3 eye, Vector3 lookAt, Vector3 vUp)
        {
            m_defaultPosition = m_position = eye;
            m_defaultLookAt = m_lookAt = lookAt;
            m_defaultUp = m_up = vUp;
            m_viewRotQuat = Quaternion.Identity;
            UpdateView();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ui"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        protected static Vector2 GetVector(UIElement ui, MouseEventArgs e)
        {
            var p = e.GetPosition(ui);
            return new Vector2((float)p.X, (float)(ui.RenderSize.Height - p.Y));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="dp"></param>
        /// <param name="ui"></param>
        /// <returns></returns>
        protected float GetMouseAngle(Vector2 dp, UIElement ui)
        {
            float div = (float)Math.Max(ui.RenderSize.Width, ui.RenderSize.Height) / 2;
            if (div < 1)
                div = 1;

            float angle = dp.Length() / div;
            return angle;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="dp"></param>
        protected virtual void KeyMove(Vector3 dp)
        {
            if (!EnableYAxisMovement)
                dp.Y = 0;
            dp *= MoveScaler;
            dp = Matrix.RotationQuaternion(m_viewRotQuat).TransformNormal(dp);
            Position += dp;
            LookAt += dp;
            UpdateView();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="angle"></param>
        protected virtual void KeyRoll(float angle)
        {
            angle *= RotationScaler;
            var m = Matrix.RotationZ(angle);
            Up = m.TransformNormal(Up);
            UpdateView();
        }

        protected virtual void KeyRotateHorizontal(float angle)
        {
            angle *= RotationScaler;

            var m = Matrix.RotationY(angle);
            Position = m.TransformNormal(Position);
            Up = m.TransformNormal(Up);
            LookAt = m.TransformNormal(LookAt);

            var m2 = Matrix.RotationZ(angle);
            Position = m2.TransformNormal(Position);
            Up = m2.TransformNormal(Up);
            LookAt = m2.TransformNormal(LookAt);

            UpdateView();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="dMouse"></param>
        protected virtual void MouseRotation(Quaternion dMouse)
        {
            var mRot = Matrix.RotationQuaternion(dMouse);

            LookAt = Position + mRot.TransformNormal(LookAt - Position);
            Up = mRot.TransformNormal(Up);

            m_viewRotQuat *= dMouse;
            m_viewRotQuat.Normalize();
        }

        /// <summary>
        ///
        /// </summary>
        protected virtual void UpdateView()
        {
            m_viewMat = Matrix.LookAtLH(m_position, m_lookAt, m_up);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        private static float GetRollSpeed(Key k)
        {
            switch (k)
            {
                case Key.E:
                    return 1;

                case Key.Q:
                    return -1;
            }
            return 0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        private static Vector3 GetSpeed(Key k)
        {
            switch (k)
            {
                case Key.W:
                case Key.Up:
                    return new Vector3(0, 0, 1);

                case Key.S:
                case Key.Down:
                    return new Vector3(0, 0, -1);

                case Key.D:
                case Key.Right:
                    return new Vector3(1, 0, 0);

                case Key.A:
                case Key.Left:
                    return new Vector3(-1, 0, 0);

                case Key.PageUp:
                    return new Vector3(0, 1, 0);

                case Key.PageDown:
                    return new Vector3(0, -1, 0);
            }
            return s_zero3;
        }

        /// <summary>
        ///
        /// </summary>
        private void OnInitInteractive()
        {
            SetScalers((float)Math.PI / 5, 3);
        }

        /// <summary>
        ///
        /// </summary>
        private void UpdateProj()
        {
            m_projMat = Matrix.PerspectiveFovLH(m_fov, m_aspect, m_nearPlane, m_farPlane);
        }
    }
}