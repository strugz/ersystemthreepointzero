import '@mdi/font/css/materialdesignicons.css';
import './app/styles/main.css';

import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import router from './app/router';
import vuetify from './app/plugins/vuetify';

createApp(App).use(createPinia()).use(router).use(vuetify).mount('#app');
