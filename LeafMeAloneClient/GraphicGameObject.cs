﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;
using SlimDX;

namespace Client
{
    /// <summary>
    /// A GameObject that renders on the screen (networked or non-networked).
    /// </summary>
    public abstract class GraphicGameObject : GameObject
    {

        //static particle system which is used for any graphics gameobject to be burned
        public static ParticleSystem Fire;

        // Model that's associated with this object.
        private Model model;


        /// <summary>
        /// Init the particle system for burning. This will only ever run once, if the particle system has not been initialized yet.
        /// </summary>
        private static void InitializeBurning()
        {
            //if the static fire is already initialized dont initialize it.
            if (Fire == null)
            {

                Fire = new FlameThrowerParticleSystem(2, 10, 2.5f, 1f, 5f)
                {
                    emissionRate = 5,
                    Enabled = true
                };
                Fire.EnableGeneration(true);
                Fire.Transform.Rotation.X = 90f.ToRadians();
                Fire.SetOrigin(Vector3.Zero);
            }
        }

        /// <summary>
        /// Creates a new graphic game object with no model. Eg a particle system. 
        /// Note: if you call initializeburning here you get infinite recursion because a particle system is also a graphics object.
        /// </summary>
        protected GraphicGameObject() : base()
        {
        }

        /// <summary>
        /// Creates a new graphic game object with specified model and position.
        /// </summary>
        /// <param name="modelPath">Path to the model for this GameObject.</param>
        protected GraphicGameObject(string modelPath) : base()
        {
            SetModel(modelPath);
            InitializeBurning();
        }

        /// <summary>
        /// Update step of this object.
        /// </summary>
        /// <param name="deltaTime">Time since last frame.</param>
        public override void Update(float deltaTime)
        {
            if (model == null)
                return;
            model.m_Properties = Transform;
            model.Update(deltaTime);
        }

        /// <summary>
        /// Draw step of the object, renders it on screen.
        /// </summary>
        public virtual void Draw()
        {
            model?.Draw();

            //if the object is currently burning, draw the fire on them.
            if (Burning)
            {
                Transform t = new Transform {Position = Transform.Position,Scale =  new Vector3(1,1,1)};
                Fire?.DrawTransform(t);
            }
        }

        /// <summary>
        /// Sets the model of this object.
        /// </summary>
        /// <param name="filePath">Path of the model.</param>
        public void SetModel(string filePath)
        {
            //Console.WriteLine(File.Exists(filePath));
            model = new Model(filePath);
            Name = filePath.Split('.')[0];
        }

        /// <summary>
        /// Destroy this GameObject and remove any references.
        /// </summary>
        public override void Destroy()
        {
            GameClient.instance.Destroy(this);
        }
    }
}
