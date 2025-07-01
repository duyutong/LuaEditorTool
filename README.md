# LuaEditorTool
利用lua的动态编译特性，在编辑器模式批量修改预制体/资源
在Extension中输入资源对应的后缀，可以针对不同资源做处理，如果需要匹配多种后缀，用 | 符号分隔

-- 这是一个替换预制体内所有Text组件字体的示例

local textComponents = prefab:GetComponentsInChildren(typeof(CS.UnityEngine.UI.Text))
local fontToReplace = asset

for i = 0, textComponents.Length - 1 do
	if textComponents[i].font:Equals(nil) or textComponents[i].font .name ~= fontToReplace.name then
		textComponents[i].font = fontToReplace
		checkDirty = true
	end
end


-- 这是一个为所有按钮添加点击音效组件的示例

local btnComponents= prefab:GetComponentsInChildren(typeof(CS.UnityEngine.UI.Button))

for i = 0, btnComponents.Length - 1 do
	local trans = btnComponents[i].transform;
	local clickSound = trans:GetComponent(typeof(CS.ClickSound)) ;
	if clickSound == nil  then
		trans.gameObject:AddComponent(typeof(CS.ClickSound))
		checkDirty = true
	end
end


-- 这是一个修改InputField设置的示例

local inputComponents= prefab:GetComponentsInChildren(typeof(CS.UnityEngine.UI.InputField))

for i = 0, inputComponents.Length - 1 do
	local input = inputComponents[i]
	if input ~= nil  and  input .contentType == CS.UnityEngine.UI.InputField. ContentType. Name then
		input .contentType = CS.UnityEngine.UI.InputField. ContentType. Standard
		checkDirty = true
	end
end

if checkDirty == true then 
	print( "changed prefab ",prefab.name)
end
