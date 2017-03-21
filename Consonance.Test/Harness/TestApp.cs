﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consonance.Test
{
    class TestApp
    {
        public TestView view = new TestView();
        public PlanCommands plan_commands = new PlanCommands();
        public TestPlatform platform = new TestPlatform();
        public TestValueRequestBuilder builder = new TestValueRequestBuilder();
        public TestInput input = new TestInput();
        public Task StartPresenter()
        {
            return Presenter.PresentTo(view, platform, input, plan_commands, builder);
        }
    }
}
