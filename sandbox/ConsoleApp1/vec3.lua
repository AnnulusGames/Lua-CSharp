Vec3 = {
    new = function(x, y, z)
        local instance = { x, y, z }
        setmetatable(instance, Vec3)
        return instance
    end,

    magnitude = function(self)
        return math.sqrt(self.x * self.x + self.y * self.y + self.z * self.z)
    end,

    __index = function(table, index)
        if index == 'x' then
            return rawget(table, 1)
        elseif index == 'y' then
            return rawget(table, 2)
        elseif index == 'z' then
            return rawget(table, 3)
        else
            error('vec3 key must be x, y or z')
        end
    end,

    __newindex = function(table, index, value)
        if index == 'x' then
            return rawset(table, 1, value)
        elseif index == 'y' then
            return rawset(table, 2, value)
        elseif index == 'z' then
            return rawset(table, 3, value)
        else
            error('vec3 key must be x, y or z')
        end
    end,

    __add = function(a, b)
        return Vec3.new(a.x + b.x, a.y + b.y, a.z + b.z)
    end,

    __sub = function(a, b)
        return Vec3.new(a.x - b.x, a.y - b.y, a.z - b.z)
    end,

    __unm = function(a)
        return Vec3.new(-a.x, -a.y, -a.z)
    end,

    __eq = function(a, b)
        return a.x == b.y and a.y == b.y and a.z == b.z
    end,

    __tostring = function(self)
        return '(' .. self.x .. ',' .. self.y .. ',' .. self.z .. ')'
    end
}

local a = Vec3.new(1, 1, 1)
local b = Vec3.new(1, 1, 1)
