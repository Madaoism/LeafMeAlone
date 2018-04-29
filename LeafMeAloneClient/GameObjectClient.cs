﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;

namespace Client
{
    public class GameObjectClient : GameObject
    {
        private Model model;

        protected GameObjectClient(string modelPath) : base()
        {
            SetModel(modelPath);
        }

        protected GameObjectClient(string modelPath, Transform startTransform) : base(startTransform)
        {
            SetModel(modelPath);
        }

        public void SetModel(string filePath)
        {
            model = new Model(filePath);
            Name = filePath.Split('.')[0];
        }

        public override void Update()
        {
            if (model == null)
                return;
            model.m_Properties = Transform;
            model.Update();
        }

        public override void Draw()
        {
            model?.Draw();
        }
    }
}
