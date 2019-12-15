﻿/* Copyright 2019 Vincent Bousquet

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
   */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexDMD.Scenes
{
    abstract class Scene : Group
    {
        protected readonly string _id;
        protected float _pauseS;
        protected AnimationType _animateIn;
        protected AnimationType _animateOut;
        protected float _time;
        public bool _active = false;

        public Scene(AnimationType animateIn, float pauseS, AnimationType animateOut, string id = "")
        {
            _animateIn = animateIn;
            _animateOut = animateOut;
            _pauseS = pauseS;
            _id = id;
        }

        public virtual void Begin()
        {
            SetSize(_parent._width, _parent._height);
            _active = true;
        }

        public virtual void End()
        {
            _active = false;
        }

        public override void Update(float delta)
        {
            base.Update(delta);
            _time += delta;
        }

        public bool IsFinished()
        {
            return _time > _pauseS;
        }
    }
}
