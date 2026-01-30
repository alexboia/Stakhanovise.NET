import { defineCollection, z } from 'astro:content';

const docs = defineCollection({
	type: 'content',
	schema: z.object({
		title: z.string(),
		summary: z.string(),
		order: z.number().optional(),
		tags: z.array(z.string()).optional()
	})
});

const presentations = defineCollection({
	type: 'content',
	schema: z.object({
		title: z.string(),
		tagline: z.string(),
		audience: z.string().optional(),
		updated: z.string().optional(),
		featured: z.boolean().optional()
	})
});

export const collections = { docs, presentations };
