\## Back-face Culling



How Elite draws solid-looking 3D ships by only drawing visible faces



One of the reasons that Elite's 3D wireframe ships look so good is because you can't see through them - they look genuinely solid. This is down to a process called "back-face culling", a mathematical process that works out which faces of the ship are visible to the viewer and which ones aren't. It then discards (or "culls") any faces that aren't visible and only draws those that we can actually see. This prevents the wireframes from being see-through, and this gives the ships and space stations a real sense of solidity:

A space station in BBC Micro cassette Elite



Back-face culling is implemented in part 5 of LL9 as part of the ship-drawing routine.



The main principle behind back-face culling is the dot product. This simple mathematical operation takes two vectors and produces a scalar value that is in essence a kind of mathematical application of one vector to the other, of taking the "essence" of one vector and applying it to another. For the purposes of back-face culling in Elite, we use the dot product in two distinct ways.

Direction of the face normal

\----------------------------



The first use is to calculate whether a face is pointing towards us or away from us, so we can decide whether to draw it. For this we use a property of the dot product that's almost tailor-made for solving this problem: if the dot product of two vectors is negative, then the angle between them is greater than 90 degrees (and by extension, if it is positive then the angle is less than 90 degrees, while a dot product of 0 means they are at exactly 90 degrees to each other).



Now, consider a face of one of our ships, and take the surface normal vector of that face. This is the vector that sticks out of the surface of the face at 90 degrees to the surface (so a ship with its face normals attached would look like a strange, space-faring hedgehog). Now think about rotating that face in space, and you can see that if that face normal is pointing towards us - in other words if the spike is pointing in our general direction, more towards us than away from us - then we can see the front of the face, while if the face normal (the spike) is pointing away from us, then the face is also pointing away from us and we can't see it.



The normal is effectively pointing in the direction of the face, so if it is pointing in our general direction, then the face is pointing towards us and we can see it; similarly, if the normal is pointing away from us, then the face is also pointing away from us, and we can't see it. Finally, if the face is exactly side on to us, it's on the cusp of being visible and invisible to us.



It looks like this (though you'll have to imagine that the normals are at 90 degrees, as that's pretty difficult to do in ASCII):



&#x20;                              /                        `.   /   normal points

&#x20;                             /      normal points        `./    towards us,

&#x20; line of sight ----->       /`.     away, face            /     face visible

&#x20;                           /   `.   not visible          /



The solid lines represent the faces, while the dotted lines represent the normals. On the left the normal is pointing away from us and the face is turned away from us, so it's not visible, while on the right the normal is pointing towards us and so is the face, so it's visible.



So, when the face normal is pointing away from us, the face is hidden from us. To put this another way, when the angle between the face normal and our line of sight is greater than 90 degrees, then the face normal is pointing away from us and the face is not visible. Similarly, when the angle is less than 90 degrees, the normal is pointing towards us, so the face is visible.



This means that we can calculate the visibility of a face by calculating the following dot product:



&#x20; line of sight vector . face normal vector



If the result of this dot product is negative, then the face is visible; if it's positive, it isn't visible; and if it's zero it's edge on to us.

Line of sight vector

\--------------------



We already have the face normal vector, as that's how faces are stored in the memory (see the ship blueprints documentation at XX21 for more information on this). So how can we calculate the line of sight vector?



The first solution that springs to mind is to use the vector from our position to the ship we are trying to draw. Our ship is always at the origin (0, 0, 0), so this would mean the line of sight vector would be the vector from the origin to the ship at (x, y, z), or \[x y z]. This is close, but it isn't quite right as the line of sight we are interested in is towards the face, not the centre of the ship.



Luckily, any point on the face will do for our calculation, so we just need to find the vector from our position to any point on the face, and the dot product will work fine. Indeed, we can extend that to say that any point on the plane containing the face will suffice; if you think about it, extending the face in all directions wouldn't change its visibility to us, so it won't change the calculation if we pick a point that is outside the limits of the actual face we want to draw. It just has to be in the same plane as the face for this to work.



Given this, perhaps we can get hold of a vector that goes from the ship's centre to a point on the extended face plane? As then we could define our line of sight vector by adding the vector to the ship's centre, and the vector from the ship's centre to the extended face plane.



Luckily - well, it isn't luck, it's by design, but it feels lucky - this is exactly how faces are stored in Elite. Each face is stored as a face normal vector, but that normal is specifically designed to be the vector from the centre of the ship to the nearest point on the extended face plane. The direction of the vector stored in the ship's blueprint is parallel to the face normal (the hedgehog spike), but its magnitude (i.e. the vector's length) is set to the distance from the ship's centre to the extended face plane. So, by design, we can add this vector to the ship's position in space to get the line of sight vector from our ship to a point on the face plane, like so:



&#x20; line of sight vector = \[x y z] + face normal vector



and we can then calculate the dot product of this vector with the face normal vector to get our positive or negative result, which determines whether this face is hidden or visible.



(Incidentally, the face normal vector that's stored in the ship's blueprint goes from the ship's centre to the nearest point on the extended face plane, as the vector is a normal, though we don't need to use this fact, it's just an interesting consequence of how this is all set up.)

Projecting onto the orientation vector space

\--------------------------------------------



So are we done? Not quite. The maths is all good, but the way the coordinates and vectors are stored means we have to do a bit more geometry before we can plug the values into our dot product equation.



The problem is that the vector from our position to the ship we're displaying uses a different set of axes to the ship's blueprint. As we cavort through space, pitching and rolling away, the x-axis is always pointing to the right on screen, the y-axis always points up, and the z-axis always points into the screen. This is always the case, even if we switch views (this is where the PLUT routine comes in, as it switches the axes around to maintain this relationship between the screen and the axes). When we roll, pitch and travel through space, we don't actually roll or pitch, but instead we rotate the universe around us (this is done in the MVEIT routine). On top of this, each ship has its own set of orientation vectors - nosev, roofv and sidev - that determine the direction that the ship is pointing.



The problem is that the face normal vector in the ship's blueprint is stored in terms of an unrotated ship. Ship coordinates are stored with the x, y and z axes along the orientation vectors, so if we were sitting in the ship, the x-coordinate of the face normal vector would be pointing to our right, the y-coordinate would be pointing up, and the z-coordinate would be pointing forward, because that's how the coordinates are stored. This intentionally coincides with the way the orientation vectors point; from the perspective of the pilot of the other ship, sidev is the x-axis going off to the right, roofv is the y-axis going up, and the z-axis is nosev, heading forward. The face normal vectors are stored with reference to these orientation vectors as axes; mathematically speaking, the ship's orientation vectors form an orthonormal basis for the Euclidean space in which the face normals are expressed.



So somehow we need to merge these two coordinate systems - the orientation of the universe from the perspective of our ship vs the orientation of the universe from the perspective of the other ship. Luckily there's an easy solution, and again it involves the dot product.



The feature of the dot product that we use to merge the coordinate systems is called "scalar projection". This says that given a vector and a unit vector, we can calculate the projection of the vector onto the unit vector by simply calculating the dot product. If we do this with a vector and three unit vectors, then the dot product gives us that vector, expressed in terms of the three unit vectors - in other words, this is how we convert coordinates from one set of axes to another.



In our case, we have the following equation to calculate:



&#x20; line of sight vector . face normal vector



where:



&#x20; line of sight vector = \[x y z] + face normal vector



so if we can convert \[x y z] to use the same axes as the face normal vector, then we can do our calculation. As discussed above, the face normal vector uses the ship's orientation vectors as its axes, and we know that the orientation vectors are unit vectors because they are orthonormal (and we keep calling the TIDY routine to ensure that they stay this way). So we can project the \[x y z] vector onto each of the orientation vectors in turn, like this:



&#x20; \[x y z] projected onto sidev = \[x y z] . sidev

&#x20; \[x y z] projected onto roofv = \[x y z] . roofv

&#x20; \[x y z] projected onto nosev = \[x y z] . nosev



and because the orientation vectors are effectively the x, y and z axes for the ship's space, we can combine them into our projected line of sight vector like this:



&#x20;                            \[ \[x y z] . sidev ]

&#x20; projected \[x y z] vector = \[ \[x y z] . roofv ]

&#x20;                            \[ \[x y z] . nosev ]



The final step is to combine all of the above into our final equation. This is the value we want to calculate:



&#x20; line of sight vector . face normal vector



and we know that:



&#x20; line of sight vector = \[x y z] + face normal vector



so if we now project the \[x y z] vector into the same space as the face normals so we can combine them properly, we get:



&#x20; line of sight vector = projected \[x y z] vector + face normal vector



&#x20;                        \[ \[x y z] . sidev ]   \[ normal\_x ]

&#x20;                      = \[ \[x y z] . roofv ] + \[ normal\_y ]

&#x20;                        \[ \[x y z] . nosev ]   \[ normal\_z ]



&#x20;                        \[ \[x y z] . sidev + normal\_x ]

&#x20;                      = \[ \[x y z] . roofv + normal\_y ]

&#x20;                        \[ \[x y z] . nosev + normal\_z ]



and if we substitute this into our final calculation, we can calculate the dot product as follows:



&#x20;              \[ \[x y z] . sidev + normal\_x ]   \[ normal\_x ]

&#x20; visibility = \[ \[x y z] . roofv + normal\_y ] . \[ normal\_y ]

&#x20;              \[ \[x y z] . nosev + normal\_z ]   \[ normal\_z ]



If the result is negative the face is visible, otherwise it is not visible.



This is exactly what happens in part 5 of the LL9 routine. We set the block of memory at XX15 to the left-hand side of the final calculation and the block at XX12 block to the right-hand side, and calculate the dot product XX12 . XX15 to tell us whether or not this face is visible, which we store in the table at XX2. This gets repeated for all the faces until XX2 contains the visibilities of all the faces for this ship.



\## Calculating vertex coordinates

Determining whether a ship's vertex is visible or hidden from us



To understand the following, you'll probably want to have a look through the deep dive on back-face culling, which describes how we can work out whether or not a ship's face is visible.



As part of the back-face cull, we projected the vector \[x y z] onto the orientation vector space like this:



&#x20; \[x y z] projected onto sidev = \[x y z] . sidev

&#x20; \[x y z] projected onto roofv = \[x y z] . roofv

&#x20; \[x y z] projected onto nosev = \[x y z] . nosev



We can express this exact same calculation in terms of matrix multiplication:



&#x20; \[ sidev ]   \[ x ]

&#x20; \[ roofv ] . \[ y ]

&#x20; \[ nosev ]   \[ z ]



or, expanding it out fully:



&#x20;                     \[ sidev\_x sidev\_y sidev\_z ]   \[ x ]

&#x20; projected \[x y z] = \[ roofv\_x roofv\_y roofv\_z ] . \[ y ]

&#x20;                     \[ nosev\_x nosev\_y nosev\_z ]   \[ z ]



This is just a different way of expressing the exact same equation as we used in part 5 of LL9, just with a matrix instead of individual dot products.

Transposing the rotation matrix

\-------------------------------



So the inverse matrix will map vectors in the orientation vector space back into normal ship space. The inverse of a rotation matrix is its transpose (as long as it is a unit matrix), so this is the calculation:



&#x20;                     \[ sidev\_x roofv\_x nosev\_x ]   \[ x ]

&#x20; projected \[x y z] = \[ sidev\_y roofv\_y nosev\_y ] . \[ y ]

&#x20;                     \[ sidev\_z roofv\_z nosev\_z ]   \[ z ]



This takes a vector, which goes from the ship's centre to the vertex and is expressed in terms of the ship's axes (i.e. its orientation vectors), and instead expresses it in terms of our ship's axes (i.e. our orientation vectors).



Given this new vector, we can add the vector from our ship to the other ship, to get the vector from us to the vertex, expressed in our ship's coordinates:



&#x20;                    \[ sidev\_x roofv\_x nosev\_x ]   \[ x ]   \[ x ]

&#x20; vector to vertex = \[ sidev\_y roofv\_y nosev\_y ] . \[ y ] + \[ y ]

&#x20;                    \[ sidev\_z roofv\_z nosev\_z ]   \[ z ]   \[ z ]



The code to calculate this equation takes up part 6 and part 7 of LL9. It's in two parts because there are two small subroutines that have rudely inserted themselves just before the big reveal. These are used by part 8 and don't play a part in this calculation (except to make it harder to follow).



