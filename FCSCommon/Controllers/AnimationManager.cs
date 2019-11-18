﻿using System;
using System.Collections.Generic;
using System.Text;
using FCSCommon.Abstract;
using FCSCommon.Utilities;
using UnityEngine;

namespace FCSCommon.Controllers
{
    public class AnimationManager : MonoBehaviour
    {
        #region Unity Methods   
        private void Start()
        {
            GetAnimatorComponent();

            if (Animator == null || Animator.enabled) return;
            QuickLogger.Debug("Animator was disabled and now has been enabled");
            Animator.enabled = true;
        }

        private bool GetAnimatorComponent()
        {
            Animator = GetComponentInChildren<Animator>() ?? GetComponent<Animator>();

            if (Animator == null)
            {
                QuickLogger.Error("Animator component not found on the GameObject.");
                return false;
            }

            return true;
        }

        #endregion

        #region Public Methods
        public Animator Animator { get; set; }
        #endregion

        #region public Methods
        /// <summary>
        /// Sets the an animator float to a certain value (For use with setting the page on the screen)
        /// </summary>
        /// <param name="stateHash">The hash of the parameter</param>
        /// <param name="value">Float to set</param>
        public void SetFloatHash(int stateHash, float value)
        {
            Animator.SetFloat(stateHash, value);
        }

        /// <summary>
        /// Sets the an animator boolean to a certain value
        /// </summary>
        /// <param name="stateHash">The hash of the parameter</param>
        /// <param name="value">Float to set</param>
        public void SetBoolHash(int stateHash, bool value)
        {
            if (Animator == null)
            {
                if(!GetAnimatorComponent()) return;
            }

            Animator.SetBool(stateHash, value);
        }

        /// <summary>
        /// Sets the an animator integer to a certain value
        /// </summary>
        /// <param name="stateHash">The hash of the parameter</param>
        /// <param name="value">Float to set</param>
        public void SetIntHash(int stateHash, int value)
        {
            if (Animator == null)
            {
                if (!GetAnimatorComponent()) return;
            }

            if (Animator == null) return;

            Animator.SetInteger(stateHash, value);
        }

        public int GetIntHash(int hash)
        {
            if (Animator != null) return Animator.GetInteger(hash);
            return !GetAnimatorComponent() ? 0 : Animator.GetInteger(hash);
        }

        public bool GetBoolHash(int hash)
        {
            if (Animator != null) return Animator.GetBool(hash);

            return GetAnimatorComponent() && Animator.GetBool(hash);
        }
        #endregion
    }
}
