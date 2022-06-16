<h1>Echoviser Neural Networks Lab</h1>

<h3>Some strange effects of Neural Networks</h3>

<h4>Unexplainable surges</h4>

I've found some strange effects. Even a 100% learned NN could "go crazy" at some moment and make a row of mistakes without any visible reason. It doesn't depend on if it continues learning or not. Be careful with your Tesla car :) Someday it could kill you just out of nothing. I've not found an explanation of this. Yet.

Options to check:

1. .NET memory optimization issue.

1 <img width="1280" alt="2022-06-12 (5)" src="https://user-images.githubusercontent.com/53271533/173506921-d082039b-3816-4401-a03a-49ac8b2a4926.png">
2 <img width="1280" alt="2022-06-12 (6)" src="https://user-images.githubusercontent.com/53271533/173506925-d721c70a-c37a-49d3-9cb3-5359af7d1f5d.png">
3 <img width="1280" alt="2022-06-12 (4)" src="https://user-images.githubusercontent.com/53271533/173506918-d91a5f61-da85-47f3-8d4b-c19b8598a81f.png">

<h4> Stable weights</h4>

Another one strange effect is that after some time of learning a NN gets some stable weights which don't change. But they should change, because all weights on a previous layer are changed. And the pattern of these weights looks like this. Outcoming weights are grouped at some neurons and incoming weights most often belong to some first and some last neurons of the output layer.


34 <img width="1280" alt="2022-06-12 (1)" src="https://user-images.githubusercontent.com/53271533/173506908-20448b8a-4fe6-4943-a301-8f903ffc6efd.png">
35 <img width="1280" alt="2022-06-12 (2)" src="https://user-images.githubusercontent.com/53271533/173506913-22234672-4ec9-4315-832a-30eaaceccae3.png">

<h4>Some other screenshots</h4>

36 <img width="1280" alt="2022-06-12 (8)" src="https://user-images.githubusercontent.com/53271533/173506933-1a26528c-7fb7-4df8-8ef0-a0902328f7bf.png">
4 <img width="1280" alt="2022-06-01 (2)" src="https://user-images.githubusercontent.com/53271533/173506779-c5d43a5d-e1c3-489b-a964-2436348034c5.png">
5 <img width="1280" alt="2022-06-01 (3)" src="https://user-images.githubusercontent.com/53271533/173506783-6033239f-0637-4301-bb48-8b3c158943db.png">
6 <img width="1280" alt="2022-06-01 (4)" src="https://user-images.githubusercontent.com/53271533/173506792-e02efa26-b437-4129-b11f-84889de18885.png">
7 <img width="1280" alt="2022-06-01" src="https://user-images.githubusercontent.com/53271533/173506802-c1422bcb-f3fc-4d69-84bc-771aa5c32d35.png">
8 <img width="1280" alt="2022-06-02 (1)" src="https://user-images.githubusercontent.com/53271533/173506808-a3d76bf5-d24a-4fd0-bf1f-cbdbba4c2761.png">
9 <img width="1280" alt="2022-06-02 (2)" src="https://user-images.githubusercontent.com/53271533/173506809-230101e0-8c2d-4d2f-9bb2-f4648be69ea3.png">
10 <img width="1280" alt="2022-06-02 (3)" src="https://user-images.githubusercontent.com/53271533/173506816-f02e195f-a67a-475a-a9bb-4d8ad431ae86.png">
11 <img width="1280" alt="2022-06-02 (4)" src="https://user-images.githubusercontent.com/53271533/173506818-1e3efe16-7b66-465c-b733-51e3d21806bb.png">
12 <img width="1280" alt="2022-06-02 (5)" src="https://user-images.githubusercontent.com/53271533/173506821-107f7eaf-a5cc-4142-b927-fbc771640a63.png">
13 <img width="1280" alt="2022-06-02 (6)" src="https://user-images.githubusercontent.com/53271533/173506827-dd88f222-1d00-4c71-8cbe-a4d9c556bf96.png">
14 <img width="1280" alt="2022-06-02 (7)" src="https://user-images.githubusercontent.com/53271533/173506830-bcf553c5-e1e1-464f-bc58-4196382bf4a8.png">
15 <img width="1280" alt="2022-06-02 (8)" src="https://user-images.githubusercontent.com/53271533/173506832-e12a4969-8876-4339-91fd-ccd2492fa89d.png">
16 <img width="1280" alt="2022-06-02 (9)" src="https://user-images.githubusercontent.com/53271533/173506834-647fe1bb-7ace-49e9-b99d-f594c6a04e8a.png">
17 <img width="1280" alt="2022-06-02 (10)" src="https://user-images.githubusercontent.com/53271533/173506837-65224292-7cfb-4d58-81a9-153778977329.png">
18 <img width="1280" alt="2022-06-02 (11)" src="https://user-images.githubusercontent.com/53271533/173506840-17be341c-39de-49a2-8337-d9a8ca5326b3.png">
19 <img width="1280" alt="2022-06-02 (12)" src="https://user-images.githubusercontent.com/53271533/173506842-a5a9cbd3-f704-4bf3-81b5-2eda0c6a8458.png">
20 <img width="1280" alt="2022-06-02 (15)" src="https://user-images.githubusercontent.com/53271533/173506846-e36a68a2-df29-4c2a-9c72-3aa89f4b8127.png">
21 <img width="1280" alt="2022-06-02 (16)" src="https://user-images.githubusercontent.com/53271533/173506852-1d3e0862-ebcb-4cbb-8562-e5d355f4006e.png">
22 <img width="1280" alt="2022-06-02 (17)" src="https://user-images.githubusercontent.com/53271533/173506856-d547cddb-0d8a-43dd-81d2-e2bdd97d7d50.png">
23 <img width="1280" alt="2022-06-02 (18)" src="https://user-images.githubusercontent.com/53271533/173506862-c4587025-5699-4649-bd54-3f129aba08c4.png">
24 <img width="1280" alt="2022-06-02" src="https://user-images.githubusercontent.com/53271533/173506864-b929007e-150d-47a1-b31b-1164c9f95738.png">
25 <img width="1280" alt="2022-06-03 (1)" src="https://user-images.githubusercontent.com/53271533/173506869-4e98f608-2cbb-480c-a506-31a120d17877.png">
26 <img width="1280" alt="2022-06-03 (16)" src="https://user-images.githubusercontent.com/53271533/173506874-90ef4e84-375a-4032-8caf-15807fecd9c7.png">
27 <img width="1280" alt="2022-06-03 (58)" src="https://user-images.githubusercontent.com/53271533/173506881-aaffa22e-ab9a-489f-966e-3309f7dc1329.png">
28 <img width="1280" alt="2022-06-03 (66)" src="https://user-images.githubusercontent.com/53271533/173506887-d5e5fff3-90be-4f55-9e57-f0687d14eebb.png">
29 <img width="1280" alt="2022-06-05" src="https://user-images.githubusercontent.com/53271533/173506893-5132cb9c-d162-4ab1-9cf3-f1b34aa75378.png">
30 <img width="1280" alt="2022-06-07 (1)" src="https://user-images.githubusercontent.com/53271533/173506896-18686948-9c0f-44e4-8ab3-3c11cf371791.png">
31 <img width="1280" alt="2022-06-12 (10)" src="https://user-images.githubusercontent.com/53271533/173506934-b96e99db-bf2f-4d4a-bb82-67bfab6665f4.png">
32 <img width="1280" alt="2022-06-13" src="https://user-images.githubusercontent.com/53271533/173506939-17b8690f-c5a5-4e39-9b87-2d3a6b7ed457.png">
33 <img width="1280" alt="2022-06-07 (80)" src="https://user-images.githubusercontent.com/53271533/173506905-4f032866-3868-4a7f-a196-ad9bc1d4af48.png">

