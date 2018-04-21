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

        public void SetModel(string filePath)
        {
            model = new Model(filePath);
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