﻿using SlimDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;
using SlimDX;
using System.Windows.Forms;

namespace Client
{
    /// <summary>
    /// Handles user input and calls input mapped functions. 
    /// </summary>
    class InputManager
    {
        public Dictionary<char, Action> InputMap;

        /// <summary>
        /// Constructor for the input manager. Should take in a player that will respond to input events.
        /// </summary>
        /// <param name="userPlayer"></param>
        public InputManager(PlayerClient userPlayer)
        {
            // Dictionary to keep track of what functions should be called by what key presses.
            InputMap = new Dictionary<char, Action>{
                { 'w', () => { userPlayer.RequestMove(new Vector2(0.0f, 1.0f)); } },
                { 'a', () => { userPlayer.RequestMove(new Vector2(-1.0f, 0.0f));  } },
                { 's', () => { userPlayer.RequestMove(new Vector2(0.0f, -1.0f)); } },
                { 'd', () => { userPlayer.RequestMove(new Vector2(1.0f, 0.0f));  } }
            };
        }

        // Key press event handler. Calls functions from the input map.
        public void OnKeyPress(object ignored, KeyPressEventArgs keyArg)
        {
            InputMap.TryGetValue(keyArg.KeyChar, out Action keyAction);
            keyAction();
        }

        // Mouse movement event handler.
        public void OnMouseMove(Vector2 mousePosition)
        {
            // TODO
        }
    }
}