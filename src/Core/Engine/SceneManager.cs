using System;
using System.Collections.Generic;
using Engine.UI;

namespace Engine;

public class SceneManager
{
    private List<Scene> _scenes = new List<Scene>();
    public int CurrentScene = 0;
    public int SceneCount => _scenes.Count;
    
    public Scene CreateScene()
    {
        Scene newScene = new Scene();
        _scenes.Add(newScene);

        return newScene;
    }

    public bool TryRemoveScene(int index)
    {
        if (index < 0 || index >= _scenes.Count)
        {
            return false; 
        }
        
        _scenes.RemoveAt(index);
        return true;
    }

    public bool TryGetScene(int index, out Scene result)
    {
        if (index < 0 || index >= _scenes.Count)
        {
            result = default;
            return false; 
        }

        result = _scenes[index];
        return true;
    }
}

public class Scene
{
    public SceneGraph Objects { get; private set; } = new();
}

public class SceneGraph
{
    private Dictionary<ITransformNode, ITransformNode> _childToParent = new();
    private Dictionary<ITransformNode, List<ITransformNode>> _parentToChildren = new();
    private List<ITransformNode> _roots = new();
    private Dictionary<ITransformNode, Action> _eventHandlers = new();

    public void RegisterRoot(ITransformNode node)
    {
        AddNode(node, null);
    }

    public void SetParent(ITransformNode child, ITransformNode newParent)
    {
        if (_childToParent.TryGetValue(child, out var currentParent) && currentParent == newParent)
            return;

        if (currentParent != null && _parentToChildren.TryGetValue(currentParent, out var oldSiblings))
        {
            oldSiblings.Remove(child);
            if (oldSiblings.Count == 0) _parentToChildren.Remove(currentParent);
        }
        else if (currentParent == null)
        {
            _roots.Remove(child);
        }

        AddNode(child, newParent);
    }

    private void AddNode(ITransformNode node, ITransformNode parent)
    {
        _childToParent[node] = parent;
        
        if (parent == null)
        {
            _roots.Add(node);
        }
        else
        {
            if (!_parentToChildren.ContainsKey(parent))
                _parentToChildren[parent] = new List<ITransformNode>();
            
            _parentToChildren[parent].Add(node);
        }

        if (!_eventHandlers.ContainsKey(node))
        {
            _eventHandlers[node] = () => UnregisterNode(node);
            node.OnDestroyed += _eventHandlers[node];
        }
    }

    public void UnregisterNode(ITransformNode node)
    {
        if (_eventHandlers.TryGetValue(node, out var handler))
        {
            node.OnDestroyed -= handler;
            _eventHandlers.Remove(node);
        }

        if (_childToParent.TryGetValue(node, out var parent))
        {
            if (parent == null) _roots.Remove(node);
            else if (_parentToChildren.TryGetValue(parent, out var siblings)) 
            {
                siblings.Remove(node);
                if (siblings.Count == 0) _parentToChildren.Remove(parent);
            }
            _childToParent.Remove(node);
        }

        if (_parentToChildren.TryGetValue(node, out var children))
        {
            var childrenCopy = new List<ITransformNode>(children); 
            foreach (var child in childrenCopy)
            {
                UnregisterNode(child);
            }
        }
    }

    public IReadOnlyList<ITransformNode> GetRoots() => _roots.AsReadOnly();
    
    public IReadOnlyList<ITransformNode> GetChildren(ITransformNode parent)
    {
        if (_parentToChildren.TryGetValue(parent, out var children))
            return children.AsReadOnly();
        return new List<ITransformNode>().AsReadOnly();
    }

    public bool IsRoot(ITransformNode node) => _childToParent.TryGetValue(node, out var p) && p == null;

    public void Clear()
    {
        foreach (var root in _roots)
        {
            UnregisterNode(root);    
        }
    }
}