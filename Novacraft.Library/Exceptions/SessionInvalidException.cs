﻿// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace Novacraft.Library.Exceptions;

public class SessionInvalidException : Exception
{
    public SessionInvalidException()
    {
    }

    public SessionInvalidException(string msg)
        : base(msg)
    {
    }

    public SessionInvalidException(string msg, Exception inner)
        : base(msg, inner)
    {
    }
}