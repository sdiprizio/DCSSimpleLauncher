using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Lua;
using Lua.Standard;

namespace DCSSimpleLauncher.Helper
{
    internal static class LuaParserExtension
    {
        extension(LuaTable table)
        {
            public string ToFormattedString(int indentLevel = 1, StringBuilder sb = null)
            {
                sb ??= new StringBuilder();
                string indent = new string('\t', indentLevel);
                sb.AppendLine("{");
                foreach ((LuaValue key, LuaValue value) in table)
                {
                    sb.Append(indent + $"[\"{key}\"] = ");
                    switch (value.Type)
                    {
                        case LuaValueType.String:
                            sb.Append($"\"{value}\",\n");
                            break;
                        case LuaValueType.Boolean:
                            sb.Append($"{value.ToBoolean().ToString().ToLower()},\n");
                            break;
                        case LuaValueType.Number:
                            sb.Append($"{value},\n");
                            break;
                        case LuaValueType.Table:
                            var nestedTable = value.Read<LuaTable>();
                            sb.Append(nestedTable.ToFormattedString(indentLevel + 1));
                            break;
                        default:
                            break;
                    }
                }
                sb.Append(new string('\t', indentLevel - 1) + "}");
                sb.Append(indentLevel == 1 ? "\n" : ",\n");
                return sb.ToString();
            }
        }
    }
}

/*
 For reference, below the equivalent in LUA

local function indent_str(level)
    return string.rep('  ', level)
end

local function is_array(t)
    if type(t) ~= 'table' then return false end
    local i = 0
    for k,_ in pairs(t) do
    i = i + 1
    if type(k) ~= 'number' then return false end
    end
    return true
end

local function serialize(o, level)
    level = level or 0
    local t = type(o)
    if t == 'number' or t == 'boolean' then return tostring(o) end
    if t == 'string' then return string.format('%q', o) end
    if t == 'table' then
    local indent = indent_str(level)
    local nextIndent = indent_str(level + 1)

    local parts = { }

    -- detect array-like table
    local isArray = true
    local max = 0
    for k,_ in pairs(o) do
        if type(k) ~= 'number' then isArray = false end
        if type(k) == 'number' and k > max then max = k end
    end

    if next(o) == nil then
        return '{}' -- empty table
    end

    if isArray then
        for i=1,max do
        table.insert(parts, nextIndent .. serialize(o[i], level + 1))
        end
    else
        for k,v in pairs(o) do
        table.insert(parts, nextIndent .. '[' .. serialize(k, 0) .. '] = ' .. serialize(v, level + 1))
        end
    end

    return '{\n' .. table.concat(parts, ',\n') .. '\n' .. indent .. '}'
    end
    return 'nil'
end

return serialize(options, 0)

*/