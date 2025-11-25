# TMDB 集成说明

## 功能概述

项目已集成 TMDB (The Movie Database) API，用于获取动漫的详细信息：
- ✅ 中文简介（优先显示）
- ✅ 英文简介（作为备选）
- ✅ 评分信息（0-10分）
- ✅ 评分人数
- ✅ 首播日期
- ✅ 海报图片
- ✅ 背景图片
- ✅ **异步后台加载**（不阻塞主流程）

## 开箱即用 🎉

**无需配置 API Key！** 项目已内置默认的公共 API Key，可以直接使用。

这也是为什么 Emby、Jellyfin 等工具不需要填写 TMDB Key 的原因——它们也使用了类似的公共 Key 或代理服务。

### 可选：使用自己的 API Key

如果你想获得更高的请求限制，可以使用自己的 API Key：

1. 访问 [TMDB 官网](https://www.themoviedb.org/) 注册账号
2. 进入 [API 设置页面](https://www.themoviedb.org/settings/api)
3. 申请 API Key（选择 Developer）
4. 打开 `Aria2Fast/Service/TmdbManager.cs`，替换第 27 行的 Key：

```csharp
private const string kTmdbApiKey = "你的API_KEY";
```

## 功能说明

### 🚀 异步后台加载
- **不阻塞主流程**：动漫列表立即加载显示，TMDB 信息在后台异步获取
- **渐进式增强**：TMDB 信息加载完成后自动更新 UI
- **用户体验优先**：刷新速度快，不会因为 TMDB 请求而变慢

### 💾 自动缓存
- TMDB 查询结果会自动缓存到 `tmdb_cache.json` 文件
- 缓存有效期：30 天
- 减少 API 调用次数，提高响应速度
- 有缓存的动漫会立即显示评分

### 🌏 智能简介显示
简介显示优先级：
1. **TMDB 中文简介**（如果有）
2. **TMDB 英文简介**（如果有）
3. **HTML 抓取的简介**（兜底）

### 🎨 UI 显示
在动漫详情页面中：
- 简介会自动显示最佳内容（中文优先）
- 如果有 TMDB 数据，会显示评分信息（金色星星 ★）
- 评分信息包括：评分值 + 评价人数
- TMDB 信息加载后自动更新显示

### 名称清理
为了提高搜索准确度，自动清理动漫名称：
- 移除括号内容：`()` `[]` `【】`
- 移除季数信息：`第一季` `Season 1` `S01` 等
- 移除多余空格

## 缓存管理

### 查看缓存统计
```csharp
TmdbManager.Instance.GetCacheStats()
```

### 清除所有缓存
```csharp
TmdbManager.Instance.ClearCache()
```

### 清除过期缓存（超过 30 天）
```csharp
TmdbManager.Instance.ClearExpiredCache(30)
```

## API 使用

### 手动搜索动漫
```csharp
var tmdbInfo = await TmdbManager.Instance.SearchAnimeAsync("动漫名称");
if (tmdbInfo != null)
{
    Debug.WriteLine($"中文简介: {tmdbInfo.OverviewZh}");
    Debug.WriteLine($"评分: {tmdbInfo.VoteAverage}");
}
```

### 获取最佳简介
```csharp
// 在 MikanAnime 对象上
var bestSummary = anime.BestSummary;
```

## 工作原理

### 加载流程
1. **主流程**：快速加载动漫列表和 RSS 信息
2. **后台线程**：异步获取每个动漫的 TMDB 信息
3. **缓存优先**：有缓存的动漫立即显示评分
4. **渐进更新**：新获取的 TMDB 信息实时更新到 UI
5. **智能延迟**：请求间隔 250ms，避免触发 API 限制

### 为什么不会变慢？
- ✅ 主界面立即显示，不等待 TMDB
- ✅ TMDB 请求在后台独立线程执行
- ✅ 有缓存的立即显示，无需等待
- ✅ 失败不影响原有功能

## 注意事项

1. **API 限制**：公共 Key 有共享限制，建议使用自己的 Key
2. **网络要求**：需要能访问 `api.themoviedb.org`
3. **后台加载**：TMDB 信息会逐渐加载完成，评分会陆续显示
4. **搜索准确性**：自动名称清理可能不完美，建议检查结果
5. **缓存文件**：`tmdb_cache.json` 会随着使用增大，可定期清理

## 数据模型

### TmdbAnimeInfo
```csharp
public class TmdbAnimeInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string OriginalName { get; set; }
    public string OverviewZh { get; set; }      // 中文简介
    public string OverviewEn { get; set; }      // 英文简介
    public double VoteAverage { get; set; }     // 评分 (0-10)
    public int VoteCount { get; set; }          // 评分人数
    public string FirstAirDate { get; set; }    // 首播日期
    public string PosterPath { get; set; }      // 海报路径
    public string BackdropPath { get; set; }    // 背景图路径
}
```

## 未来扩展

可以基于 TMDB 数据实现：
- 按评分排序动漫
- 显示动漫海报（替换 Mikan 图片）
- 显示首播日期
- 按类型筛选（需要额外 API 调用）
- 演职员表信息
- 季数和集数信息

## 问题排查

### API 调用失败
1. 检查 API Key 是否正确配置
2. 检查网络连接
3. 查看 Debug 输出窗口的日志

### 找不到动漫
1. TMDB 数据库可能没有该动漫
2. 名称清理导致搜索不准确
3. 尝试使用英文名称搜索

### 中文简介为空
- 某些动漫在 TMDB 上没有中文翻译
- 系统会自动降级使用英文简介
