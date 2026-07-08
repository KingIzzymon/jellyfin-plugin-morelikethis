# Jellyfin Plugin: MoreLikeThis

A plugin to customize the "More like this" list located at the bottom of Movies and Shows using a weight system.

For example, content will be displayed from highest weight to lowest weight:

| Property | Weight |
| --- | --- |
| Same collection | 100 |
| Same tags | 75 |
| Same studio | 20 |
| Same genre | 10 |

Note: The weights per each property can be customized from the plugin's configuration screen.
Note: Weights can be assigned to combinations of properties (e.g. "Same studio" & "Same genre")

Available properties include:

- Collection
- Tags
- Studio
- Genre

Note: If content genre's dont have a pre-determined weight based on genre order, the following genres will include a 1.1x weight multiplier:

- Animation
- Cartoon
- Childrens

