local function c()
    for i = 1, 100 do
        coroutine.yield(i)
    end

    return 1000
end

local co = coroutine.create(c)

local x = 0
while coroutine.status(co) ~= "dead" do
    local _, i = coroutine.resume(co)
    x = x + i
end

return x
