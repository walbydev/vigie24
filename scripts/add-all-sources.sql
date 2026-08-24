-- ============================================
-- Script d'ajout de 200+ sources RSS
-- Date: 2026-08-18
-- Description: Adds a broad selection of technology RSS sources
--              (French and international)
-- ============================================

BEGIN;

-- ============================================
-- CATEGORY: General News (France)
-- ============================================
INSERT INTO sources ("Name", "FeedUrl", "WebsiteUrl", "SourceKind", "IngestionMethod", "IsActive") VALUES
('01net', 'https://www.01net.com/feed/', 'https://www.01net.com/', 'Press', 'Rss', true),
('Numerama', 'https://www.numerama.com/feed/', 'https://www.numerama.com/', 'Press', 'Rss', true),
('Journal du Net', 'https://www.journaldunet.com/feed/', 'https://www.journaldunet.com/', 'Press', 'Rss', true),
('ZDNet France', 'https://www.zdnet.fr/rss.xml', 'https://www.zdnet.fr/', 'Press', 'Rss', true),
('Les Echos Start', 'https://lesEchos.fr/startup/feed/', 'https://startup.lesechos.fr/', 'Press', 'Rss', true),
('Usine Digitale', 'https://www.usine-digitale.fr/rss.xml', 'https://www.usine-digitale.fr/', 'Press', 'Rss', true),
('Silicon.fr', 'https://www.silicon.fr/feed/', 'https://www.silicon.fr/', 'Press', 'Rss', true),
('Clubic', 'https://www.clubic.com/feed/', 'https://www.clubic.com/', 'Press', 'Rss', true),
('Frandroid', 'https://www.frandroid.com/feed/', 'https://www.frandroid.com/', 'Press', 'Rss', true),
('Les Numériques', 'https://www.lesnumeriques.com/feed/', 'https://www.lesnumeriques.com/', 'Press', 'Rss', true)
;

-- ============================================
-- CATEGORY: IT Security
-- ============================================
INSERT INTO sources ("Name", "FeedUrl", "WebsiteUrl", "SourceKind", "IngestionMethod", "IsActive") VALUES
('SecurityWeek', 'https://www.securityweek.com/feed/', 'https://www.securityweek.com/', 'Press', 'Rss', true),
('The Hacker News', 'https://thehackernews.com/feeds/posts/default', 'https://thehackernews.com/', 'Press', 'Rss', true),
('Krebs on Security', 'https://krebsonsecurity.com/feed/', 'https://krebsonsecurity.com/', 'BlogLab', 'Rss', true),
('Schneier on Security', 'https://www.schneier.com/feed/', 'https://www.schneier.com/', 'BlogLab', 'Rss', true),
('DarkReading', 'https://www.darkreading.com/rss/', 'https://www.darkreading.com/', 'Press', 'Rss', true),
('BleepingComputer', 'https://www.bleepingcomputer.com/feed/', 'https://www.bleepingcomputer.com/', 'Press', 'Rss', true),
('Threatpost', 'https://threatpost.com/feed/', 'https://threatpost.com/', 'Press', 'Rss', true),
('SANS ISC', 'https://isc.sans.edu/feed/', 'https://isc.sans.edu/', 'BlogLab', 'Rss', true),
('CERT-FR', 'https://www.cert.ssi.gouv.fr/uploads/feed/', 'https://www.cert.ssi.gouv.fr/', 'Press', 'Rss', true),
('Security Affairs', 'http://securityaffairs.co/wordpress/feed/', 'https://securityaffairs.co/', 'Press', 'Rss', true),
('CyberScoop', 'https://www.cyberscoop.com/feed/', 'https://www.cyberscoop.com/', 'Press', 'Rss', true),
('Infosec Magazine', 'https://infosecmagazine.com/feed/', 'https://infosecmagazine.com/', 'Press', 'Rss', true)
;

-- ============================================
-- CATÉGORIE: Open Source
-- ============================================
INSERT INTO sources ("Name", "FeedUrl", "WebsiteUrl", "SourceKind", "IngestionMethod", "IsActive") VALUES
('OpenSource.com', 'https://opensource.com/feed', 'https://opensource.com/', 'BlogLab', 'Rss', true),
('Framablog', 'https://framablog.org/feed', 'https://framablog.org/', 'BlogLab', 'Rss', true),
('The Register', 'https://www.theregister.com/headlines.atom', 'https://www.theregister.com/', 'Press', 'Rss', true),
('LWN.net', 'https://lwn.net/headlines/rss', 'https://lwn.net/', 'BlogLab', 'Rss', true),
('Hacker News', 'https://news.ycombinator.com/rss', 'https://news.ycombinator.com/', 'BlogLab', 'Rss', true),
('Reddit r/programming', 'https://www.reddit.com/r/programming/.rss', 'https://www.reddit.com/r/programming/', 'Reddit', 'Rss', true),
('Reddit r/linux', 'https://www.reddit.com/r/linux/.rss', 'https://www.reddit.com/r/linux/', 'Reddit', 'Rss', true),
('GitHub Trending', 'https://github.com/trending?l=', 'https://github.com/trending', 'BlogLab', 'Rss', true),
('GitLab Blog', 'https://about.gitlab.com/blog/feed/', 'https://about.gitlab.com/', 'BlogLab', 'Rss', true),
('Bitbucket Blog', 'https://bitbucket.org/blog/rss', 'https://bitbucket.org/', 'BlogLab', 'Rss', true),
('SourceForge News', 'https://sourceforge.net/blog/feed/', 'https://sourceforge.net/', 'Press', 'Rss', true),
('FOSS Force', 'https://fossforce.com/feed/', 'https://fossforce.com/', 'BlogLab', 'Rss', true),
('Linux Foundation', 'https://www.linuxfoundation.org/blog/feed/', 'https://www.linuxfoundation.org/', 'BlogLab', 'Rss', true)
;

-- ============================================
-- CATÉGORIE: DevOps & Cloud
-- ============================================
INSERT INTO sources ("Name", "FeedUrl", "WebsiteUrl", "SourceKind", "IngestionMethod", "IsActive") VALUES
('DevOps.com', 'https://devops.com/feed/', 'https://devops.com/', 'Press', 'Rss', true),
('CNCF Blog', 'https://www.cncf.io/blog/feed/', 'https://www.cncf.io/', 'BlogLab', 'Rss', true),
('Kubernetes Blog', 'https://kubernetes.io/blog/feed/', 'https://kubernetes.io/', 'BlogLab', 'Rss', true),
('Docker Blog', 'https://www.docker.com/blog/feed/', 'https://www.docker.com/', 'BlogLab', 'Rss', true),
('AWS Blog', 'https://aws.amazon.com/blogs/aws/feed/', 'https://aws.amazon.com/', 'BlogLab', 'Rss', true),
('Azure Blog', 'https://azure.microsoft.com/en-us/blog/feed/', 'https://azure.microsoft.com/', 'BlogLab', 'Rss', true),
('Google Cloud Blog', 'https://cloud.google.com/blog/topics/rss', 'https://cloud.google.com/', 'BlogLab', 'Rss', true),
('HashiCorp Blog', 'https://www.hashicorp.com/blog/feed/', 'https://www.hashicorp.com/', 'BlogLab', 'Rss', true),
('Red Hat Blog', 'https://www.redhat.com/en/blog/rss', 'https://www.redhat.com/', 'BlogLab', 'Rss', true),
('SUSE Blog', 'https://www.suse.com/c/feed/', 'https://www.suse.com/', 'BlogLab', 'Rss', true),
('VMware Blog', 'https://blogs.vmware.com/feed/', 'https://www.vmware.com/', 'BlogLab', 'Rss', true),
('DigitalOcean Tutorials', 'https://www.digitalocean.com/community/feed/', 'https://www.digitalocean.com/', 'BlogLab', 'Rss', true),
('Linode Blog', 'https://www.linode.com/blog/feed/', 'https://www.linode.com/', 'BlogLab', 'Rss', true),
('Cloud Native Computing', 'https://www.cncf.io/feed/', 'https://www.cncf.io/', 'BlogLab', 'Rss', true),
('Container Journal', 'https://containerjournal.com/feed/', 'https://containerjournal.com/', 'Press', 'Rss', true),
('DevOps Digest', 'https://devopsdigest.com/feed/', 'https://devopsdigest.com/', 'Press', 'Rss', true)
;

-- ============================================
-- CATÉGORIE: IA & Data
-- ============================================
INSERT INTO sources ("Name", "FeedUrl", "WebsiteUrl", "SourceKind", "IngestionMethod", "IsActive") VALUES
('AI Trends', 'https://aitrends.com/feed/', 'https://aitrends.com/', 'Press', 'Rss', true),
('Towards Data Science', 'https://medium.com/feed/@towardsdatascience', 'https://towardsdatascience.com/', 'BlogLab', 'Rss', true),
('Kaggle Blog', 'https://www.kaggle.com/discussions/feed', 'https://www.kaggle.com/', 'BlogLab', 'Rss', true),
('TensorFlow Blog', 'https://blog.tensorflow.org/feed/', 'https://blog.tensorflow.org/', 'BlogLab', 'Rss', true),
('PyTorch Blog', 'https://pytorch.org/blog/rss/', 'https://pytorch.org/', 'BlogLab', 'Rss', true),
('Hugging Face Blog', 'https://huggingface.co/blog/feed', 'https://huggingface.co/', 'BlogLab', 'Rss', true),
('OpenAI Blog', 'https://openai.com/blog/rss/', 'https://openai.com/', 'BlogLab', 'Rss', true),
('DeepMind Blog', 'https://deepmind.google/discover/blog/feed/', 'https://deepmind.google/', 'BlogLab', 'Rss', true),
('Google AI Blog', 'https://ai.googleblog.com/feed/', 'https://ai.googleblog.com/', 'BlogLab', 'Rss', true),
('MIT Tech Review AI', 'https://www.technologyreview.com/topic/artificial-intelligence/feed/', 'https://www.technologyreview.com/', 'Press', 'Rss', true),
('Analytics Vidhya', 'https://www.analyticsvidhya.com/blog/feed/', 'https://www.analyticsvidhya.com/', 'BlogLab', 'Rss', true),
('KDnuggets', 'https://www.kdnuggets.com/feed/', 'https://www.kdnuggets.com/', 'Press', 'Rss', true),
('Data Science Central', 'https://www.datasciencecentral.com/profiles/blogs/feed', 'https://www.datasciencecentral.com/', 'Press', 'Rss', true),
('Machine Learning Mastery', 'https://machinelearningmastery.com/feed/', 'https://machinelearningmastery.com/', 'BlogLab', 'Rss', true),
('Artificial Intelligence News', 'https://www.artificialintelligence-news.com/feed/', 'https://www.artificialintelligence-news.com/', 'Press', 'Rss', true),
('The Batch (DeepLearning.AI)', 'https://www.deeplearning.ai/the-batch/feed/', 'https://www.deeplearning.ai/', 'BlogLab', 'Rss', true),
('Lex Fridman Podcast', 'https://lexfridman.com/feed/', 'https://lexfridman.com/', 'BlogLab', 'Rss', true)
;

-- ============================================
-- CATÉGORIE: Programmation
-- ============================================
INSERT INTO sources ("Name", "FeedUrl", "WebsiteUrl", "SourceKind", "IngestionMethod", "IsActive") VALUES
('Dev.to', 'https://dev.to/feed', 'https://dev.to/', 'BlogLab', 'Rss', true),
('Stack Overflow Blog', 'https://stackoverflow.blog/feed/', 'https://stackoverflow.blog/', 'BlogLab', 'Rss', true),
('GitHub Blog', 'https://github.blog/feed/', 'https://github.blog/', 'BlogLab', 'Rss', true),
('CSS-Tricks', 'https://css-tricks.com/feed/', 'https://css-tricks.com/', 'BlogLab', 'Rss', true),
('Smashing Magazine', 'https://www.smashingmagazine.com/feed/', 'https://www.smashingmagazine.com/', 'BlogLab', 'Rss', true),
('A List Apart', 'https://alistapart.com/feed/', 'https://alistapart.com/', 'BlogLab', 'Rss', true),
('SitePoint', 'https://www.sitepoint.com/feed/', 'https://www.sitepoint.com/', 'BlogLab', 'Rss', true),
('DZone', 'https://dzone.com/rss', 'https://dzone.com/', 'Press', 'Rss', true),
('InfoQ', 'https://www.infoq.com/feed/', 'https://www.infoq.com/', 'Press', 'Rss', true),
('The New Stack', 'https://thenewstack.io/feed/', 'https://thenewstack.io/', 'Press', 'Rss', true),
('FreeCodeCamp News', 'https://www.freecodecamp.org/news/rss', 'https://www.freecodecamp.org/', 'BlogLab', 'Rss', true),
('CodeProject', 'https://www.codeproject.com/rss', 'https://www.codeproject.com/', 'BlogLab', 'Rss', true),
('Dr Dobbs', 'https://www.drdobbs.com/rss', 'https://www.drdobbs.com/', 'Press', 'Rss', true),
('ACM Queue', 'https://queue.acm.org/feed/', 'https://queue.acm.org/', 'BlogLab', 'Rss', true),
('IEEE Spectrum', 'https://spectrum.ieee.org/rss', 'https://spectrum.ieee.org/', 'Press', 'Rss', true),
('ACM Communications', 'https://cacm.acm.org/feed/', 'https://cacm.acm.org/', 'BlogLab', 'Rss', true),
('Real Python', 'https://realpython.com/feed/', 'https://realpython.com/', 'BlogLab', 'Rss', true),
('Python Bytes', 'https://pythonbytes.fm/feed/', 'https://pythonbytes.fm/', 'BlogLab', 'Rss', true),
('JavaScript Weekly', 'https://javascriptweekly.com/feed/', 'https://javascriptweekly.com/', 'BlogLab', 'Rss', true),
('Node Weekly', 'https://nodeweekly.com/feed/', 'https://nodeweekly.com/', 'BlogLab', 'Rss', true),
('Go Weekly', 'https://goweekly.com/feed/', 'https://goweekly.com/', 'BlogLab', 'Rss', true),
('Rust Weekly', 'https://rustweekly.com/feed/', 'https://rustweekly.com/', 'BlogLab', 'Rss', true),
('Swift Weekly', 'https://swiftweeklybrief.com/feed/', 'https://swiftweeklybrief.com/', 'BlogLab', 'Rss', true),
('Kotlin Weekly', 'https://kotlinweekly.net/feed/', 'https://kotlinweekly.net/', 'BlogLab', 'Rss', true),
('.NET Weekly', 'https://dotnetweekly.com/feed/', 'https://dotnetweekly.com/', 'BlogLab', 'Rss', true),
('Java Magazine', 'https://www.oracle.com/java/technologies/javase/javamagazine/feed/', 'https://www.oracle.com/java/', 'Press', 'Rss', true),
('Spring Blog', 'https://spring.io/blog/feed/', 'https://spring.io/', 'BlogLab', 'Rss', true),
('Angular Blog', 'https://blog.angular.dev/feed/', 'https://angular.dev/', 'BlogLab', 'Rss', true),
('React Blog', 'https://react.dev/blog/rss', 'https://react.dev/', 'BlogLab', 'Rss', true),
('Vue Blog', 'https://blog.vuejs.org/feed/', 'https://blog.vuejs.org/', 'BlogLab', 'Rss', true),
('Vue.js News', 'https://news.vuejs.org/feed/', 'https://news.vuejs.org/', 'BlogLab', 'Rss', true),
('React Native Blog', 'https://reactnative.dev/blog/rss', 'https://reactnative.dev/', 'BlogLab', 'Rss', true),
('Next.js Blog', 'https://nextjs.org/blog/rss', 'https://nextjs.org/', 'BlogLab', 'Rss', true),
('Svelte Blog', 'https://svelte.dev/blog/feed', 'https://svelte.dev/', 'BlogLab', 'Rss', true)
;

-- ============================================
-- CATÉGORIE: Hardware & Tech
-- ============================================
INSERT INTO sources ("Name", "FeedUrl", "WebsiteUrl", "SourceKind", "IngestionMethod", "IsActive") VALUES
('AnandTech', 'https://www.anandtech.com/feed', 'https://www.anandtech.com/', 'Press', 'Rss', true),
('Tom''s Hardware', 'https://www.tomshardware.com/feeds/all', 'https://www.tomshardware.com/', 'Press', 'Rss', true),
('Ars Technica', 'https://feeds.arstechnica.com/arstechnica/index', 'https://arstechnica.com/', 'Press', 'Rss', true),
('The Verge Tech', 'https://www.theverge.com/rss/tech/index.xml', 'https://www.theverge.com/', 'Press', 'Rss', true),
('CNET', 'https://www.cnet.com/rss/', 'https://www.cnet.com/', 'Press', 'Rss', true),
('Engadget', 'https://www.engadget.com/rss.xml', 'https://www.engadget.com/', 'Press', 'Rss', true),
('Gizmodo', 'https://gizmodo.com/rss', 'https://gizmodo.com/', 'Press', 'Rss', true),
('TechCrunch', 'https://techcrunch.com/feed/', 'https://techcrunch.com/', 'Press', 'Rss', true),
('Wired Tech', 'https://www.wired.com/feed/rss', 'https://www.wired.com/', 'Press', 'Rss', true),
('MIT Tech Review', 'https://www.technologyreview.com/feed/', 'https://www.technologyreview.com/', 'Press', 'Rss', true),
('Hackaday', 'https://hackaday.com/feed/', 'https://hackaday.com/', 'BlogLab', 'Rss', true),
('Make: Magazine', 'https://makezine.com/feed/', 'https://makezine.com/', 'BlogLab', 'Rss', true),
('Adafruit', 'https://feeds.feedburner.com/Adafruit Industries', 'https://www.adafruit.com/', 'BlogLab', 'Rss', true),
('Hackster.io', 'https://www.hackster.io/news/rss', 'https://www.hackster.io/', 'BlogLab', 'Rss', true),
('EE Times', 'https://www.eetimes.com/rss/', 'https://www.eetimes.com/', 'Press', 'Rss', true),
('Electronic Design', 'https://www.electronicdesign.com/rss', 'https://www.electronicdesign.com/', 'Press', 'Rss', true),
('EDN Network', 'https://www.edn.com/rss/', 'https://www.edn.com/', 'Press', 'Rss', true),
('Intel News', 'https://www.intel.com/content/dam/www/public/us/en/documents/rss/news.xml', 'https://www.intel.com/', 'Press', 'Rss', true),
('AMD News', 'https://www.amd.com/en/newsroom/rss', 'https://www.amd.com/', 'Press', 'Rss', true),
('NVIDIA Blog', 'https://blogs.nvidia.com/blog/feed/', 'https://blogs.nvidia.com/', 'BlogLab', 'Rss', true),
('ARM Blog', 'https://www.arm.com/company/news/rss', 'https://www.arm.com/', 'BlogLab', 'Rss', true),
('Qualcomm News', 'https://www.qualcomm.com/news/rss', 'https://www.qualcomm.com/', 'Press', 'Rss', true),
('IBM Research', 'https://research.ibm.com/blog/feed', 'https://research.ibm.com/', 'BlogLab', 'Rss', true)
;

-- ============================================
-- CATÉGORIE: Startups & Business
-- ============================================
INSERT INTO sources ("Name", "FeedUrl", "WebsiteUrl", "SourceKind", "IngestionMethod", "IsActive") VALUES
('VentureBeat', 'https://venturebeat.com/feed/', 'https://venturebeat.com/', 'Press', 'Rss', true),
('Crunchbase News', 'https://news.crunchbase.com/feed/', 'https://news.crunchbase.com/', 'Press', 'Rss', true),
('AngelList', 'https://angel.co/feed', 'https://angel.co/', 'BlogLab', 'Rss', true),
('Product Hunt', 'https://www.producthunt.com/feed', 'https://www.producthunt.com/', 'BlogLab', 'Rss', true),
('Y Combinator News', 'https://news.ycombinator.com/rss', 'https://news.ycombinator.com/', 'BlogLab', 'Rss', true),
('Startup News', 'https://www.startupnews.com/feed/', 'https://www.startupnews.com/', 'Press', 'Rss', true),
('Business Insider Tech', 'https://www.businessinsider.com/tech/rss', 'https://www.businessinsider.com/', 'Press', 'Rss', true),
('Forbes Tech', 'https://www.forbes.com/technology/feed/', 'https://www.forbes.com/', 'Press', 'Rss', true),
('Fortune Tech', 'https://fortune.com/feed/technology/', 'https://fortune.com/', 'Press', 'Rss', true),
('Bloomberg Tech', 'https://feeds.bloomberg.com/technology/news.rss', 'https://www.bloomberg.com/technology', 'Press', 'Rss', true),
('Reuters Tech', 'https://feeds.reuters.com/reuters/technologyNews', 'https://www.reuters.com/technology/', 'Press', 'Rss', true),
('CNBC Tech', 'https://www.cnbc.com/id/10001185/device/rss', 'https://www.cnbc.com/technology/', 'Press', 'Rss', true),
('Wall Street Journal Tech', 'https://feeds.a.dj.com/rss/RSSMarketsMain.xml', 'https://www.wsj.com/', 'Press', 'Rss', true),
('Financial Times Tech', 'https://www.ft.com/technology?format=rss', 'https://www.ft.com/', 'Press', 'Rss', true),
('The Information', 'https://www.theinformation.com/feed', 'https://www.theinformation.com/', 'Press', 'Rss', true),
('Protocol', 'https://www.protocol.com/feed', 'https://www.protocol.com/', 'Press', 'Rss', true),
('Semafor', 'https://www.semafor.com/feed', 'https://www.semafor.com/', 'Press', 'Rss', true),
('Rest of World', 'https://restofworld.org/feed/', 'https://restofworld.org/', 'BlogLab', 'Rss', true),
('Stratechery', 'https://stratechery.com/feed/', 'https://stratechery.com/', 'BlogLab', 'Rss', true),
('Daring Fireball', 'https://daringfireball.net/feeds/thoughts', 'https://daringfireball.net/', 'BlogLab', 'Rss', true),
('First Round Review', 'https://firstround.com/review/feed/', 'https://firstround.com/', 'BlogLab', 'Rss', true),
('a16z Podcast', 'https://a16z.com/feed/', 'https://a16z.com/', 'BlogLab', 'Rss', true),
('Sequoia Capital', 'https://www.sequoiacap.com/article/rss/', 'https://www.sequoiacap.com/', 'BlogLab', 'Rss', true),
('Y Combinator Blog', 'https://www.ycombinator.com/blog/feed/', 'https://www.ycombinator.com/', 'BlogLab', 'Rss', true),
('Paul Graham Essays', 'https://paulgraham.com/articles.rss', 'http://www.paulgraham.com/', 'BlogLab', 'Rss', true),
('Naval Blog', 'https://nav.al/feed', 'https://nav.al/', 'BlogLab', 'Rss', true),
('Balaji Srinivasan', 'https://www.balajis.com/feed', 'https://www.balajis.com/', 'BlogLab', 'Rss', true),
('Elad Gil Blog', 'https://www.eladgil.com/feed', 'https://www.eladgil.com/', 'BlogLab', 'Rss', true)
;

-- ============================================
-- CATÉGORIE: Blockchain & Crypto
-- ============================================
INSERT INTO sources ("Name", "FeedUrl", "WebsiteUrl", "SourceKind", "IngestionMethod", "IsActive") VALUES
('CoinDesk', 'https://www.coindesk.com/arc/outboundfeeds/rss/', 'https://www.coindesk.com/', 'Press', 'Rss', true),
('Cointelegraph', 'https://cointelegraph.com/rss', 'https://cointelegraph.com/', 'Press', 'Rss', true),
('Decrypt', 'https://decrypt.co/feed', 'https://decrypt.co/', 'Press', 'Rss', true),
('The Block', 'https://www.theblock.co/rss', 'https://www.theblock.co/', 'Press', 'Rss', true),
('Bitcoin Magazine', 'https://bitcoinmagazine.com/feed', 'https://bitcoinmagazine.com/', 'Press', 'Rss', true),
('Ethereum Blog', 'https://blog.ethereum.org/feed', 'https://blog.ethereum.org/', 'BlogLab', 'Rss', true),
('Vitalik Blog', 'https://vitalik.ca/feed', 'https://vitalik.ca/', 'BlogLab', 'Rss', true),
('ConsenSys Blog', 'https://consensys.net/blog/feed/', 'https://consensys.net/', 'BlogLab', 'Rss', true),
('Chainlink Blog', 'https://blog.chain.link/feed/', 'https://blog.chain.link/', 'BlogLab', 'Rss', true),
('Polygon Blog', 'https://polygon.technology/blog/feed', 'https://polygon.technology.com/', 'BlogLab', 'Rss', true),
('Solana Blog', 'https://solana.com/news/rss', 'https://solana.com/', 'BlogLab', 'Rss', true),
('Cardano Blog', 'https://iohk.io/en/blog/rss', 'https://iohk.io/', 'BlogLab', 'Rss', true),
('Avalanche Blog', 'https://www.avax.network/blog/rss', 'https://www.avax.network/', 'BlogLab', 'Rss', true),
('Polkadot Blog', 'https://polkadot.network/feed/', 'https://polkadot.network/', 'BlogLab', 'Rss', true),
('Cosmos Blog', 'https://blog.cosmos.network/feed/', 'https://blog.cosmos.network/', 'BlogLab', 'Rss', true),
('Binance Blog', 'https://www.binance.com/en/feed/all-posts', 'https://www.binance.com/', 'BlogLab', 'Rss', true),
('CryptoSlate', 'https://cryptoslate.com/feed/', 'https://cryptoslate.com/', 'Press', 'Rss', true),
('BeInCrypto', 'https://beincrypto.com/feed/', 'https://beincrypto.com/', 'Press', 'Rss', true),
('CryptoNews', 'https://cryptonews.com/news/feed/', 'https://cryptonews.com/', 'Press', 'Rss', true),
('Blockworks', 'https://blockworks.co/feed', 'https://blockworks.co/', 'Press', 'Rss', true),
('The Defiant', 'https://www.thedefiant.io/feed', 'https://www.thedefiant.io/', 'BlogLab', 'Rss', true),
('Bankless', 'https://bankless.com/feed', 'https://bankless.com/', 'BlogLab', 'Rss', true),
('Uniswap Blog', 'https://uniswap.org/blog/feed', 'https://uniswap.org/', 'BlogLab', 'Rss', true),
('Aave Blog', 'https://aave.com/blog/feed/', 'https://aave.com/', 'BlogLab', 'Rss', true),
('Compound Blog', 'https://compound.finance/feed', 'https://compound.finance/', 'BlogLab', 'Rss', true),
('MakerDAO Blog', 'https://blog.makerdao.com/feed/', 'https://blog.makerdao.com/', 'BlogLab', 'Rss', true),
('Curve Blog', 'https://curve.fi/feed', 'https://curve.fi/', 'BlogLab', 'Rss', true),
('Lido Blog', 'https://blog.lido.fi/feed/', 'https://blog.lido.fi/', 'BlogLab', 'Rss', true),
('OpenSea Blog', 'https://opensea.io/blog/feed', 'https://opensea.io/', 'BlogLab', 'Rss', true),
('Rarible Blog', 'https://rarible.com/blog/feed', 'https://rarible.com/', 'BlogLab', 'Rss', true),
('NFT Now', 'https://nftnow.com/feed/', 'https://nftnow.com/', 'Press', 'Rss', true),
('Messari', 'https://messari.io/rss', 'https://messari.io/', 'BlogLab', 'Rss', true),
('Glassnode', 'https://glassnode.com/feed', 'https://glassnode.com/', 'BlogLab', 'Rss', true),
('Dune Analytics', 'https://dune.com/feed', 'https://dune.com/', 'BlogLab', 'Rss', true)
;

-- ============================================
-- CATÉGORIE: YouTube Tech
-- ============================================
INSERT INTO sources ("Name", "FeedUrl", "WebsiteUrl", "SourceKind", "IngestionMethod", "IsActive") VALUES
('Marques Brownlee', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCBJycsmduvYEL83R_U4JriQ', 'https://www.youtube.com/@mkbhd', 'Youtube', 'Rss', true),
('Linus Tech Tips', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCXuqSBlHAE6Xw-yeJA0Tunw', 'https://www.youtube.com/@LinusTechTips', 'Youtube', 'Rss', true),
('Unbox Therapy', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCsTcErEG8YkP2fWV5EbuiyA', 'https://www.youtube.com/@UnboxTherapy', 'Youtube', 'Rss', true),
('Dave2D', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCVYamHliCI9rw1tHR1xbkfw', 'https://www.youtube.com/@Dave2D', 'Youtube', 'Rss', true),
('Austin Evans', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCXGgrKt94gR6lmN4aN3mveA', 'https://www.youtube.com/@austinev', 'Youtube', 'Rss', true),
('JerryRigEverything', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCWFKCr40YwOZQPr85W0GvoQ', 'https://www.youtube.com/@JerryRigEverything', 'Youtube', 'Rss', true),
('Techquickie', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC0vBXGSyV14uvJ4hECDOl0Q', 'https://www.youtube.com/@techquickie', 'Youtube', 'Rss', true),
('Gamers Nexus', 'https://www.youtube.com/feeds/videos.xml?channel_id=UChErZC1xj6I09t6M99VEBqA', 'https://www.youtube.com/@GamersNexus', 'Youtube', 'Rss', true),
('Hardware Unboxed', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCI81n2L6jV7YQa7TfCqKk7A', 'https://www.youtube.com/@HardwareUnboxed', 'Youtube', 'Rss', true),
('JayzTwoCents', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCkWQ0gDrqOCarmUKmppD7LQ', 'https://www.youtube.com/@JayzTwoCents', 'Youtube', 'Rss', true),
('Bitwit', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC0LpRqQ6xXhXf0GjJbLqE5A', 'https://www.youtube.com/@bitwit', 'Youtube', 'Rss', true),
('Level1Techs', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCJ0-OtVpF0wOKEqT2Z1HEtA', 'https://www.youtube.com/@Level1Techs', 'Youtube', 'Rss', true),
('The Verge', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCddiUEpeqJcYeBxX1IVBKvQ', 'https://www.youtube.com/@TheVerge', 'Youtube', 'Rss', true),
('Vox', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCLXo7UDZvByw2ixzpQCufnA', 'https://www.youtube.com/@Vox', 'Youtube', 'Rss', true),
('Wendover Productions', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC9RM-iSvTu1uPJbIWYGaQBQ', 'https://www.youtube.com/@WendoverProductions', 'Youtube', 'Rss', true),
('Real Engineering', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCR1IuLEqb6UEA_zQ81kwXfg', 'https://www.youtube.com/@RealEngineering', 'Youtube', 'Rss', true),
('Practical Engineering', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCQMQw5S6LjKqFhHJx7fDjEw', 'https://www.youtube.com/@PracticalEngineering', 'Youtube', 'Rss', true),
('Mark Rober', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCY1kMZp36IQSyNx_9h4mpCg', 'https://www.youtube.com/@MarkRober', 'Youtube', 'Rss', true),
('Veritasium', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCHnyfMqiRRG1u-2MsSQLbXA', 'https://www.youtube.com/@Veritasium', 'Youtube', 'Rss', true),
('Vsauce', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC6nSFpj9HTCZ5t-N3Rm3-HA', 'https://www.youtube.com/@Vsauce', 'Youtube', 'Rss', true),
('Kurzgesagt', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCsXVk37bltHxD1rDPwtNM8Q', 'https://www.youtube.com/@Kurzgesagt', 'Youtube', 'Rss', true),
('TED', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCAuUUnT6oDeK4E6iLq3njJg', 'https://www.youtube.com/@TED', 'Youtube', 'Rss', true),
('3Blue1Brown', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCYO_jab_esuFRV4b17AJtAw', 'https://www.youtube.com/@3Blue1Brown', 'Youtube', 'Rss', true),
('Numberphile', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCoxcjq-8xIDTYp3uz647V5A', 'https://www.youtube.com/@Numberphile', 'Youtube', 'Rss', true),
('SmarterEveryDay', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC6107grRI4m0o2-emgoDnAA', 'https://www.youtube.com/@SmarterEveryDay', 'Youtube', 'Rss', true),
('SciShow', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCZYTClx2T1of7BRZ86-8fow', 'https://www.youtube.com/@SciShow', 'Youtube', 'Rss', true),
('CrashCourse', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCX6b17PVsYBQ0ip5gyeme-Q', 'https://www.youtube.com/@CrashCourse', 'Youtube', 'Rss', true),
('AsapSCIENCE', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCC552Sd-3nyi_tk2BudLUzA', 'https://www.youtube.com/@AsapSCIENCE', 'Youtube', 'Rss', true),
('MinutePhysics', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCUHW94eEFW7hkUMVaZz4eDg', 'https://www.youtube.com/@MinutePhysics', 'Youtube', 'Rss', true),
('freeCodeCamp', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC8butISFwT-Wl7EV0hUK0BQ', 'https://www.youtube.com/@freeCodeCamp', 'Youtube', 'Rss', true),
('Traversy Media', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC29ju8bIPH5as8OGnQzwJyA', 'https://www.youtube.com/@TraversyMedia', 'Youtube', 'Rss', true),
('The Net Ninja', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCW5YEUERMbsO407pUwKjJhg', 'https://www.youtube.com/@NetNinja', 'Youtube', 'Rss', true),
('Academind', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCSJbGkTsoDy8lJ1472d3Jqw', 'https://www.youtube.com/@Academind', 'Youtube', 'Rss', true),
('Fireship', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCsBjURrPoezykLs9EqgamOA', 'https://www.youtube.com/@Fireship', 'Youtube', 'Rss', true),
('Web Dev Simplified', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCFbNIlppjAuEX4znoulh0Cw', 'https://www.youtube.com/@WebDevSimplified', 'Youtube', 'Rss', true),
('Programming with Mosh', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCWv7vMbMWH420cc7-FzTZmA', 'https://www.youtube.com/@ProgrammingWithMosh', 'Youtube', 'Rss', true),
('Corey Schafer', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCCezIgC97PvUuR4_gbFUs5g', 'https://www.youtube.com/@CoreySchafer', 'Youtube', 'Rss', true),
('Tech With Tim', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC4JX40jDee_tINbkjycV4Sg', 'https://www.youtube.com/@TechWithTim', 'Youtube', 'Rss', true),
('ArjanCodes', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCGr1CHksWH0Ji4X8tWZJvMg', 'https://www.youtube.com/@ArjanCodes', 'Youtube', 'Rss', true),
('Hussein Nasser', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC_rjMEjZ91fYt6d9v9V8e5A', 'https://www.youtube.com/@HusseinNasser', 'Youtube', 'Rss', true),
('Theo - t3.gg', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCsBjURrPoezykLs9EqgamOA', 'https://www.youtube.com/@t3gg', 'Youtube', 'Rss', true),
('Kevin Powell', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCJZv4d5rbIKd4QHMPkcABCw', 'https://www.youtube.com/@KevinPowell', 'Youtube', 'Rss', true),
('Jack Herrington', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCd6MoB9NC6uYN2grvUNT-Zg', 'https://www.youtube.com/@JackHerrington', 'Youtube', 'Rss', true)
;

-- ============================================
-- VÉRIFICATION
-- ============================================

-- Show the total number of inserted sources
SELECT COUNT(*) as total_sources FROM sources WHERE "IsActive" = true;

-- Show the breakdown by SourceKind
SELECT "SourceKind", COUNT(*) as count FROM sources WHERE "IsActive" = true GROUP BY "SourceKind" ORDER BY count DESC;

-- Show the breakdown by IngestionMethod
SELECT "IngestionMethod", COUNT(*) as count FROM sources WHERE "IsActive" = true GROUP BY "IngestionMethod" ORDER BY count DESC;

-- Show the first 10 sources
SELECT "Name", "WebsiteUrl", "SourceKind" FROM sources WHERE "IsActive" = true ORDER BY "Name" LIMIT 10;

-- Commit de la transaction
COMMIT;

-- Message de confirmation
SELECT '✓ Ajout terminé avec succès !' as message;