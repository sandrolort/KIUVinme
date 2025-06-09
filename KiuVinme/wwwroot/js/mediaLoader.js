class ContentManager {
    constructor() {
        this.mediaList = [];
        this.currentMedia1 = null;
        this.currentMedia2 = null;

        this.initializeContent();
    }

    async initializeContent() {
        try {
            await this.loadMediaList();
            this.loadContent1();
            this.loadContent2();
        } catch (error) {
            console.error('Content initialization error:', error);
            this.loadDefaultContent();
        }
    }

    async loadMediaList() {
        try {
            console.log("IM GOATEDDDDDDDDDDDD");
            const response = await fetch('media/GetAllAds');
            
            console.log('API Response status:', response.status);
            console.log('API Response ok:', response.ok);

            if (response.ok) {
                const ads = await response.json();
                console.log('Raw API response:', ads);

                const dbVideos = ads
                    .filter(ad => ad.isActive && ad.imageUrl)
                    .map(ad => ({
                        name: ad.imageUrl.split('/').pop() || `video_${ad.uid}`,
                        url: ad.imageUrl,
                        uid: ad.uid
                    }));

                if (dbVideos.length > 0) {
                    this.mediaList = dbVideos;
                    console.log('Loaded media from database:', dbVideos);
                } else {
                    console.warn('No active ads found in database');
                    this.loadFallbackMedia();
                }
            } else {
                console.error(`API request failed with status: ${response.status}`);
                this.loadFallbackMedia();
            }

        } catch (error) {
            console.error('Error loading media from API:', error);
            this.loadFallbackMedia();
        }
    }

    loadFallbackMedia() {
        this.mediaList = [
            {
                name: 'placeholder1',
                url: 'data:video/mp4;base64,',
                isPlaceholder: true
            }
        ];
        console.log('Using fallback media');
    }

    loadDefaultContent() {
        this.createPlaceholder('1');
        this.createPlaceholder('2');
    }

    createPlaceholder(id) {
        const container = document.getElementById(`container${id}`);
        const title = document.getElementById(`title${id}`);

        container.innerHTML = `
            <div class="content-placeholder">
                <div style="text-align: center; color: #ccc;">
                    <i class="fas fa-video" style="font-size: 48px; margin-bottom: 10px;"></i>
                    <div>Content Loading...</div>
                    <div style="font-size: 12px; margin-top: 5px;">API Connection Failed</div>
                </div>
            </div>
        `;
        title.textContent = 'Loading Content';
    }

    getRandomMedia(exclude = null) {
        if (this.mediaList.length === 0) return null;

        let availableMedia = this.mediaList;
        if (exclude && this.mediaList.length > 1) {
            availableMedia = this.mediaList.filter(media =>
                media.name !== exclude.name && media.uid !== exclude.uid
            );
        }

        return availableMedia[Math.floor(Math.random() * availableMedia.length)];
    }

    loadContent1() {
        const media = this.getRandomMedia(this.currentMedia1);
        if (!media) {
            this.createPlaceholder('1');
            return;
        }
        this.currentMedia1 = media;
        this.createPlayer('1', media);
    }

    loadContent2() {
        const media = this.getRandomMedia(this.currentMedia2);
        if (!media) {
            this.createPlaceholder('2');
            return;
        }
        this.currentMedia2 = media;
        this.createPlayer('2', media);
    }

    createPlayer(id, media) {
        if (media.isPlaceholder) {
            this.createPlaceholder(id);
            return;
        }

        const container = document.getElementById(`container${id}`);
        const title = document.getElementById(`title${id}`);

        container.innerHTML = '';

        const video = document.createElement('video');
        video.className = 'content-player';
        video.muted = true;
        video.autoplay = true;
        video.loop = false;
        video.playsInline = true;
        video.style.pointerEvents = 'none';
        video.disablePictureInPicture = true;
        video.preload = 'metadata';

        video.src = media.url;

        video.addEventListener('loadstart', () => {
            console.log(`Video ${id} started loading: ${media.name}`);
        });

        video.addEventListener('canplay', () => {
            console.log(`Video ${id} can play: ${media.name}`);
            video.play().catch(err => {
                console.warn(`Autoplay failed for video ${id}:`, err);
                setTimeout(() => video.play().catch(() => {}), 1000);
            });
        });

        video.addEventListener('ended', () => {
            console.log(`Video ${id} ended, loading new content`);
            setTimeout(() => {
                if (id === '1') {
                    this.loadContent1();
                } else {
                    this.loadContent2();
                }
            }, 500); 
        });

        video.addEventListener('error', (e) => {
            console.error(`Video ${id} error:`, e, `URL: ${media.url}`);
            setTimeout(() => {
                if (id === '1') {
                    this.loadContent1();
                } else {
                    this.loadContent2();
                }
            }, 3000);
        });

        container.appendChild(video);

        if (id === '1') this.video1 = video;
        else this.video2 = video;
    }

    // async refreshContent() {
    //     console.log('Manually refreshing content...');
    //     await this.loadMediaList();
    //     this.loadContent1();
    //     this.loadContent2();
    // }
}