using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Sprite;

public class AnimationSprite
{
    private static SpriteBatch _spriteBatch;
    private static bool isInit = false;
    public Texture2D SpriteList;
    public Rectangle[,] Sprites;
    public float FrameTime;
    private float _timer = 0;
    private int _currentSprite = 0;

    private AnimationSprite(Texture2D spriteList, Rectangle[,] sprites, float frameTime)
    {
        if (spriteList == null || sprites == null)
        {
            Debug.Error("Error: spriteList or sprites is null");
        }

        Sprites = sprites;
        SpriteList = spriteList;
        FrameTime = frameTime;
    }

    public static void Load(SpriteBatch spriteBatch)
    {
        if (isInit) { return; }
        isInit = true;
        _spriteBatch = spriteBatch;
    }

    public static AnimationSprite CreateAnimationSprite(Texture2D spriteList, Vector2 spriteSize, float frameTime, bool manyAnimations = false)
    {
        Rectangle[,] Sprites = new Rectangle[(int)(spriteList.Height / spriteSize.Y), (int)(spriteList.Width / spriteSize.X)];
        for (int j = 0; j < spriteList.Height / spriteSize.Y; j++)
        {
            for (int i = 0; i < spriteList.Width / spriteSize.X; i++)
            {
                Sprites[j, i] = new Rectangle((int)(i * spriteSize.X), (int)(j * spriteSize.Y), (int)spriteSize.X, (int)spriteSize.Y);
            }
        }
        return new AnimationSprite(spriteList, Sprites, frameTime);
    }

    public void PaintAnimation(GameTime gameTime, Vector2 position, Vector2 size, int animationId = 0, SpriteEffects spriteEffects = SpriteEffects.None, float layerDepth = 0)
    {
        _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (FrameTime <= _timer)
        {
            _currentSprite++;
            _timer = 0;
        }

        _spriteBatch.Draw(SpriteList, position, Sprites[animationId, _currentSprite], Color.White, 0, Vector2.Zero, size, spriteEffects, layerDepth);

        if (_currentSprite >= Sprites.GetLength(1) - 1) { _currentSprite = 0; }
    }
}

public class TileMap
{
    public Texture2D TileMapTexture;
    private Dictionary<int, Rectangle> _texturesRect;
    private Vector2 _tileSize;
    private Color[] _color;

    public TileMap(Texture2D tileMapTexture, Vector2? tileSize = null)
    {
        TileMapTexture = tileMapTexture;
        _tileSize = tileSize ?? new Vector2(16, 16);

        if (tileMapTexture.Width % _tileSize.X != 0 || tileMapTexture.Height % _tileSize.Y != 0)
        {
            Debug.Error("Error: TileMapTexture size is not divisible by tileSize");
            return;
        }

        _color = new Color[tileMapTexture.Width * tileMapTexture.Height];
        tileMapTexture.GetData(_color);

        _texturesRect = new();
        for (int j = 0; j < tileMapTexture.Height / _tileSize.Y; j++)
        {
            for (int i = 0; i < tileMapTexture.Width / _tileSize.X; i++)
            {
                if (!IsEmptyRect((int)(i * _tileSize.X), (int)(j * _tileSize.Y)))
                {
                    _texturesRect.Add(j * (TileMapTexture.Width / (int)_tileSize.X) + i, new Rectangle((int)(i * _tileSize.X), (int)(j * _tileSize.Y), (int)_tileSize.X, (int)_tileSize.Y));
                }
            }
        }
    }

    private bool IsEmptyRect(int idX, int idY)
    {
        for (int i = 0; i < _tileSize.X; i++)
        {
            for (int j = 0; j < _tileSize.Y; j++)
            {
                int x = idX + i;
                int y = idY + j;
                int index = y * TileMapTexture.Width + x;

                if (_color[index].A != 0)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public Rectangle GetRect(int id)
    {  
        if (!_texturesRect.ContainsKey(id)) 
        {
            Debug.Error($"Index was not present in the dictionary. Id: {id}");
            return new Rectangle(0, 0, 0, 0);
        }              
        
        return _texturesRect[id];
    }

    public Rectangle GetRect(int x, int y)
    {
        int index = y * (TileMapTexture.Width / (int)_tileSize.X) + x;
        if (!_texturesRect.ContainsKey(index)) 
        { 
            Debug.Error($"Index was not present in the dictionary. Id: [{x}, {y}]");
            return new Rectangle(0, 0, 0, 0); 
        }

        return _texturesRect[index];
    }

    public int GetLength() => _texturesRect.Count;
}